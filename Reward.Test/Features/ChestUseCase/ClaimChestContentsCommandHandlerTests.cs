using FluentAssertions;
using Moq;
using Reward.Application.Features.ChestUseCase.ClaimChestContents;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Features.ChestUseCase;

public sealed class ClaimChestContentsCommandHandlerTests
{
    private readonly Guid heroId = Guid.NewGuid();
    private readonly Guid dungeonRunId = Guid.NewGuid();
    private readonly Guid chestId = Guid.NewGuid();
    private readonly Mock<IChestLootRepository> chestLootRepository = new();
    private readonly Mock<IRewardRepository> rewardRepository = new();
    private readonly Mock<IInventoryRepository> inventoryRepository = new();

    [Fact]
    public async Task Handle_FilledChestAndEnoughCapacity_TransfersEveryRewardToTheInventory()
    {
        // Arrange
        RewardEntity reward = CreateReward(Guid.Empty);
        RewardItem sword = AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        RewardItem potion = AddRewardItem(reward, "POTION", stackable: true, quantity: 3);
        SetupReward(reward);
        Inventory inventory = SetupInventory(itemCapacity: 40);

        // Act
        ClaimChestContentsResult result = await CreateHandler()
            .Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.ChestId.Should().Be(chestId);
        result.State.Should().Be("EMPTY");
        result.AlreadyEmpty.Should().BeFalse();
        result
            .Items.Select(item => new { item.ItemId, item.Quantity })
            .Should()
            .BeEquivalentTo([
                new { sword.ItemId, Quantity = 1 },
                new { potion.ItemId, Quantity = 3 },
            ]);
        inventory
            .ItemInstances.Select(itemInstance => itemInstance.Id)
            .Should()
            .BeEquivalentTo(result.Items.Select(item => item.ItemInstanceId));
        reward.HeroId.Should().Be(heroId);
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Exactly(2)
        );
        chestLootRepository.Verify(
            repository =>
                repository.AcquireGenerationLockAsync(
                    dungeonRunId,
                    chestId,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_EmptyChest_ReturnsEmptyStateWithoutTransferringAnything()
    {
        // Arrange
        RewardEntity reward = CreateReward(Guid.NewGuid());
        AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        SetupReward(reward);

        // Act
        ClaimChestContentsResult result = await CreateHandler()
            .Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.ChestId.Should().Be(chestId);
        result.State.Should().Be("EMPTY");
        result.Items.Should().BeEmpty();
        result.AlreadyEmpty.Should().BeTrue();
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
    }

    [Fact]
    public async Task Handle_UnknownChest_ThrowsNotFound()
    {
        // Arrange
        rewardRepository
            .Setup(repository =>
                repository.GetChestRewardAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((RewardEntity?)null);

        // Act
        Func<Task> action = () =>
            CreateHandler().Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Chest '*");
    }

    [Fact]
    public async Task Handle_UnknownInventory_ThrowsNotFoundAndKeepsTheChestFilled()
    {
        // Arrange
        RewardEntity reward = CreateReward(Guid.Empty);
        AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        SetupReward(reward);
        inventoryRepository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((Inventory?)null);

        // Act
        Func<Task> action = () =>
            CreateHandler().Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Inventory *");
        reward.HeroId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NotEnoughCapacity_ThrowsWithoutAddingAnyItem()
    {
        // Arrange
        RewardEntity reward = CreateReward(Guid.Empty);
        AddRewardItem(reward, "SWORD", stackable: false, quantity: 2);
        SetupReward(reward);
        SetupInventory(itemCapacity: 1);

        // Act
        Func<Task> action = () =>
            CreateHandler().Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        await action
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*item capacity of 1*");
        reward.HeroId.Should().Be(Guid.Empty);
        inventoryRepository.Verify(
            repository => repository.AddItemInstance(It.IsAny<ItemInstance>()),
            Times.Never
        );
    }

    [Fact]
    public void Validate_EmptyIdentifiers_ReturnsAnErrorForEachIdentifier()
    {
        // Arrange
        var validator = new ClaimChestContentsCommandValidator();

        // Act
        var result = validator.Validate(
            new ClaimChestContentsCommand(Guid.Empty, Guid.Empty, Guid.Empty)
        );

        // Assert
        result
            .Errors.Select(error => error.PropertyName)
            .Should()
            .BeEquivalentTo([
                nameof(ClaimChestContentsCommand.HeroId),
                nameof(ClaimChestContentsCommand.DungeonRunId),
                nameof(ClaimChestContentsCommand.ChestId),
            ]);
    }

    private ClaimChestContentsCommandHandler CreateHandler() =>
        new(
            chestLootRepository.Object,
            rewardRepository.Object,
            inventoryRepository.Object,
            new FixedClock()
        );

    private ClaimChestContentsCommand CreateCommand() => new(heroId, dungeonRunId, chestId);

    private void SetupReward(RewardEntity reward) =>
        rewardRepository
            .Setup(repository =>
                repository.GetChestRewardAsync(dungeonRunId, chestId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(reward);

    private Inventory SetupInventory(int itemCapacity)
    {
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = itemCapacity,
            PotionCapacity = 20,
        };
        inventoryRepository
            .Setup(repository =>
                repository.GetByHeroIdForUpdateAsync(heroId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(inventory);
        return inventory;
    }

    private RewardEntity CreateReward(Guid rewardHeroId) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = rewardHeroId,
            RunId = dungeonRunId,
            RewardKey = $"chest:{dungeonRunId:N}:{chestId:N}",
            Status = RewardStatus.Applied,
        };

    private static RewardItem AddRewardItem(
        RewardEntity reward,
        string category,
        bool stackable,
        int quantity
    )
    {
        Guid itemId = Guid.NewGuid();
        var rewardItem = new RewardItem
        {
            Id = Guid.NewGuid(),
            RewardId = reward.Id,
            Reward = reward,
            ItemId = itemId,
            Item = new Item
            {
                Id = itemId,
                Stackable = stackable,
                Category = new Category { Label = category },
            },
            Quantity = quantity,
        };
        reward.Items.Add(rewardItem);
        return rewardItem;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    }
}
