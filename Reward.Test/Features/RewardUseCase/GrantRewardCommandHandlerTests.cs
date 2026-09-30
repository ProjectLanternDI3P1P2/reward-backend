using FluentAssertions;
using Moq;
using Reward.Application.Features.RewardUseCase.GrantReward;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Exceptions;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Features.RewardUseCase;

public sealed class GrantRewardCommandHandlerTests
{
    private readonly Guid heroId = Guid.NewGuid();
    private readonly Guid runId = Guid.NewGuid();
    private readonly Guid causeId = Guid.NewGuid();
    private readonly Item sword = CreateItem("WEAPON");
    private readonly Item potion = CreateItem("POTION");
    private readonly Inventory inventory;
    private readonly Mock<IRewardRepository> rewardRepository = new();
    private readonly Mock<IInventoryRepository> inventoryRepository = new();
    private readonly CollectingSink logSink = new();

    public GrantRewardCommandHandlerTests()
    {
        inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = 40,
            PotionCapacity = 20,
        };
        inventoryRepository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(inventory);
        inventoryRepository
            .Setup(repository =>
                repository.GetItemByIdAsync(sword.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(sword);
        inventoryRepository
            .Setup(repository =>
                repository.GetItemByIdAsync(potion.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(potion);
        rewardRepository
            .Setup(repository =>
                repository.GetSourceByNameAsync("MONSTER", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new RewardSource { Id = Guid.NewGuid(), Name = "MONSTER" });
    }

    [Fact]
    public async Task Handle_NewOperation_CreditsEveryConfiguredItemInItsQuantityOnce()
    {
        // Arrange
        GrantRewardCommandHandler handler = CreateHandler();

        // Act
        GrantRewardResult result = await handler.Handle(
            CreateCommand(new GrantRewardItem(sword.Id, 1), new GrantRewardItem(potion.Id, 3)),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.AlreadyGranted.Should().BeFalse();
        inventory
            .ItemInstances.Select(instance => (instance.ItemId, instance.Quantity))
            .Should()
            .BeEquivalentTo([(sword.Id, 1), (potion.Id, 3)]);
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Exactly(2)
        );
        rewardRepository.Verify(
            repository =>
                repository.Add(
                    It.Is<RewardEntity>(reward =>
                        reward.Status == RewardStatus.Applied
                        && reward.HeroId == heroId
                        && reward.RewardKey == $"MONSTER:{causeId}:{heroId}"
                        && reward.Items.Count == 2
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_AlreadyGrantedOperation_ReturnsPreviousResultWithoutCreditingAgain()
    {
        // Arrange
        RewardEntity existing = CreateAppliedReward((sword.Id, 1));
        rewardRepository
            .Setup(repository =>
                repository.GetByKeyAsync(existing.RewardKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);
        GrantRewardCommandHandler handler = CreateHandler();

        // Act
        GrantRewardResult result = await handler.Handle(
            CreateCommand(new GrantRewardItem(sword.Id, 1)),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.AlreadyGranted.Should().BeTrue();
        result.RewardId.Should().Be(existing.Id);
        inventoryRepository.Verify(
            repository =>
                repository.GetByHeroIdForUpdateAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_GrantedWhileWaitingForInventoryLock_DoesNotCreditAgain()
    {
        // Arrange
        RewardEntity existing = CreateAppliedReward((sword.Id, 1));
        rewardRepository
            .SetupSequence(repository =>
                repository.GetByKeyAsync(existing.RewardKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((RewardEntity?)null)
            .ReturnsAsync(existing);
        GrantRewardCommandHandler handler = CreateHandler();

        // Act
        GrantRewardResult result = await handler.Handle(
            CreateCommand(new GrantRewardItem(sword.Id, 1)),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.AlreadyGranted.Should().BeTrue();
        inventory.ItemInstances.Should().BeEmpty();
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_DuplicateOperation_LogsTheIgnoredDuplicate()
    {
        // Arrange
        RewardEntity existing = CreateAppliedReward((sword.Id, 1));
        rewardRepository
            .Setup(repository =>
                repository.GetByKeyAsync(existing.RewardKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);
        GrantRewardCommandHandler handler = CreateHandler();

        // Act
        await handler.Handle(
            CreateCommand(new GrantRewardItem(sword.Id, 1)),
            TestContext.Current.CancellationToken
        );

        // Assert
        LogEvent logEvent = logSink.Events.Should().ContainSingle().Subject;
        logEvent.MessageTemplate.Text.Should().StartWith("Duplicate reward operation");
        logEvent.Properties["RewardKey"].Should().Be(new ScalarValue(existing.RewardKey));
    }

    [Fact]
    public async Task Handle_SameOperationWithDifferentContent_ThrowsWithoutCrediting()
    {
        // Arrange
        RewardEntity existing = CreateAppliedReward((sword.Id, 1));
        rewardRepository
            .Setup(repository =>
                repository.GetByKeyAsync(existing.RewardKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);
        GrantRewardCommandHandler handler = CreateHandler();

        // Act
        Func<Task> act = () =>
            handler.Handle(
                CreateCommand(new GrantRewardItem(sword.Id, 2)),
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_InventoryFull_ThrowsWithoutRecordingTheReward()
    {
        // Arrange
        inventory.ItemCapacity = 0;
        GrantRewardCommandHandler handler = CreateHandler();

        // Act
        Func<Task> act = () =>
            handler.Handle(
                CreateCommand(new GrantRewardItem(sword.Id, 1)),
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<InventoryCapacityExceededException>();
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_UnknownItem_ThrowsKeyNotFoundException()
    {
        // Arrange
        GrantRewardCommandHandler handler = CreateHandler();

        // Act
        Func<Task> act = () =>
            handler.Handle(
                CreateCommand(new GrantRewardItem(Guid.NewGuid(), 1)),
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private GrantRewardCommandHandler CreateHandler() =>
        new(
            rewardRepository.Object,
            inventoryRepository.Object,
            new FixedClock(),
            new LoggerConfiguration().WriteTo.Sink(logSink).CreateLogger()
        );

    private GrantRewardCommand CreateCommand(params GrantRewardItem[] items) =>
        new(heroId, runId, RewardSourceType.Monster, causeId, items);

    private RewardEntity CreateAppliedReward(params (Guid ItemId, int Quantity)[] items) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            RunId = runId,
            Status = RewardStatus.Applied,
            RewardKey = RewardEntity.CreateKey(RewardSourceType.Monster, causeId, heroId),
            Items = items
                .Select(item => new RewardItem
                {
                    ItemId = item.ItemId,
                    ItemInstanceId = Guid.NewGuid(),
                    Quantity = item.Quantity,
                })
                .ToList(),
        };

    private static Item CreateItem(string category) =>
        new()
        {
            Id = Guid.NewGuid(),
            Category = new Category { Label = category },
        };

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
