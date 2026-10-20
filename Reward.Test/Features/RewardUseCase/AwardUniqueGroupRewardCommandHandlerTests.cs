using FluentAssertions;
using Moq;
using Reward.Application.Features.RewardUseCase.AwardUniqueGroupReward;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using Reward.Domain.ValueObjects;
using Serilog;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Features.RewardUseCase;

public sealed class AwardUniqueGroupRewardCommandHandlerTests
{
    private readonly Guid runId = Guid.NewGuid();
    private readonly Guid causeId = Guid.NewGuid();
    private readonly Item legendarySword = new()
    {
        Id = Guid.NewGuid(),
        Name = "Legendary Sword",
        Category = new Category { Label = "WEAPON" },
        Rarity = new Rarity { Label = "LEGENDARY" },
    };
    private readonly ActivityParticipant first = CreateParticipant(ParticipationStatus.Active);
    private readonly ActivityParticipant left = CreateParticipant(ParticipationStatus.Left);
    private readonly ActivityParticipant second = CreateParticipant(ParticipationStatus.Dead);
    private readonly Dictionary<Guid, Inventory> inventories = [];
    private readonly Mock<IRewardRepository> rewardRepository = new();
    private readonly Mock<IInventoryRepository> inventoryRepository = new();
    private readonly Mock<IRandomSource> random = new();

    public AwardUniqueGroupRewardCommandHandlerTests()
    {
        foreach (ActivityParticipant participant in new[] { first, left, second })
        {
            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                HeroId = participant.HeroId,
                ItemCapacity = 40,
                PotionCapacity = 20,
            };
            inventories[participant.HeroId] = inventory;
            inventoryRepository
                .Setup(repository =>
                    repository.GetByHeroIdForUpdateAsync(
                        participant.HeroId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(inventory);
        }

        inventoryRepository
            .Setup(repository =>
                repository.GetItemByIdAsync(legendarySword.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(legendarySword);
        rewardRepository
            .Setup(repository =>
                repository.GetSourceByNameAsync("BOSS", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new RewardSource { Id = Guid.NewGuid(), Name = "BOSS" });
    }

    [Fact]
    public async Task Handle_NewUniqueReward_AwardsItToExactlyOneEligiblePlayer()
    {
        // Arrange
        random.Setup(source => source.NextInt32(2)).Returns(1);
        AwardUniqueGroupRewardCommandHandler handler = CreateHandler();

        // Act
        AwardUniqueGroupRewardResult result = await handler.Handle(
            CreateCommand(),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.AlreadyAwarded.Should().BeFalse();
        result.RecipientHeroId.Should().Be(second.HeroId);
        inventories[second.HeroId].ItemInstances.Should().ContainSingle();
        inventories[first.HeroId].ItemInstances.Should().BeEmpty();
        inventories[left.HeroId].ItemInstances.Should().BeEmpty();
        rewardRepository.Verify(
            repository =>
                repository.Add(
                    It.Is<RewardEntity>(reward =>
                        reward.HeroId == second.HeroId
                        && reward.Status == RewardStatus.Applied
                        && reward.RewardKey == $"BOSS:{causeId}:UNIQUE:{legendarySword.Id}"
                        && reward.Items.Single().ItemNameSnapshot == "Legendary Sword"
                        && reward.Items.Single().ItemRaritySnapshot == "LEGENDARY"
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_NewUniqueReward_LocksTheRewardKeyBeforeDrawing()
    {
        // Arrange
        var calls = new List<string>();
        rewardRepository
            .Setup(repository =>
                repository.LockKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .Callback(() => calls.Add("lock"))
            .Returns(Task.CompletedTask);
        random
            .Setup(source => source.NextInt32(It.IsAny<int>()))
            .Callback(() => calls.Add("draw"))
            .Returns(0);
        AwardUniqueGroupRewardCommandHandler handler = CreateHandler();

        // Act
        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        calls.Should().Equal("lock", "draw");
    }

    [Fact]
    public async Task Handle_AlreadyAwarded_ReturnsSameRecipientWithoutDrawingAgain()
    {
        // Arrange
        RewardEntity existing = CreateAwardedReward(first.HeroId, quantity: 1);
        rewardRepository
            .Setup(repository =>
                repository.GetByKeyAsync(existing.RewardKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);
        AwardUniqueGroupRewardCommandHandler handler = CreateHandler();

        // Act
        AwardUniqueGroupRewardResult result = await handler.Handle(
            CreateCommand(),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.AlreadyAwarded.Should().BeTrue();
        result.RecipientHeroId.Should().Be(first.HeroId);
        random.Verify(source => source.NextInt32(It.IsAny<int>()), Times.Never);
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
    public async Task Handle_AwardedWhileWaitingForTheLock_DoesNotAwardItToAnotherPlayer()
    {
        // Arrange
        RewardEntity existing = CreateAwardedReward(first.HeroId, quantity: 1);
        rewardRepository
            .SetupSequence(repository =>
                repository.GetByKeyAsync(existing.RewardKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((RewardEntity?)null)
            .ReturnsAsync(existing);
        AwardUniqueGroupRewardCommandHandler handler = CreateHandler();

        // Act
        AwardUniqueGroupRewardResult result = await handler.Handle(
            CreateCommand(),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.AlreadyAwarded.Should().BeTrue();
        result.RecipientHeroId.Should().Be(first.HeroId);
        random.Verify(source => source.NextInt32(It.IsAny<int>()), Times.Never);
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_SameUniqueRewardWithDifferentQuantity_ThrowsInvalidOperationException()
    {
        // Arrange
        RewardEntity existing = CreateAwardedReward(first.HeroId, quantity: 2);
        rewardRepository
            .Setup(repository =>
                repository.GetByKeyAsync(existing.RewardKey, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existing);
        AwardUniqueGroupRewardCommandHandler handler = CreateHandler();

        // Act
        Func<Task> act = () =>
            handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_NoEligiblePlayer_ThrowsWithoutAwarding()
    {
        // Arrange
        AwardUniqueGroupRewardCommandHandler handler = CreateHandler();

        // Act
        Func<Task> act = () =>
            handler.Handle(
                CreateCommand() with
                {
                    Participants = [left],
                },
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        rewardRepository.Verify(
            repository => repository.Add(It.IsAny<RewardEntity>()),
            Times.Never
        );
    }

    private AwardUniqueGroupRewardCommandHandler CreateHandler() =>
        new(
            rewardRepository.Object,
            inventoryRepository.Object,
            random.Object,
            new FixedClock(),
            new LoggerConfiguration().CreateLogger()
        );

    private AwardUniqueGroupRewardCommand CreateCommand() =>
        new(runId, RewardSourceType.Boss, causeId, legendarySword.Id, 1, [first, left, second]);

    private RewardEntity CreateAwardedReward(Guid heroId, int quantity) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            RunId = runId,
            Status = RewardStatus.Applied,
            RewardKey = RewardEntity.CreateUniqueKey(
                RewardSourceType.Boss,
                causeId,
                legendarySword.Id
            ),
            Items =
            [
                new RewardItem
                {
                    ItemId = legendarySword.Id,
                    ItemInstanceId = Guid.NewGuid(),
                    Quantity = quantity,
                },
            ],
        };

    private static ActivityParticipant CreateParticipant(ParticipationStatus status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), status);

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    }
}
