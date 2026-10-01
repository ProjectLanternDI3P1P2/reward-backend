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
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid heroId = Guid.NewGuid();
    private readonly Guid dungeonRunId = Guid.NewGuid();
    private readonly Guid chestId = Guid.NewGuid();
    private readonly Mock<IChestLootRepository> chestLootRepository = new();
    private readonly Mock<IInventoryRepository> inventoryRepository = new();

    [Fact]
    public async Task Handle_FilledChestAndEnoughCapacity_TransfersEveryRewardToTheInventory()
    {
        // Arrange
        RewardEntity reward = SetupChest(Guid.Empty);
        RewardItem sword = AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        RewardItem potion = AddRewardItem(reward, "POTION", stackable: true, quantity: 3);
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
    public async Task Handle_NonStackableReward_CreatesOneItemInstancePerUnit()
    {
        // Arrange
        RewardEntity reward = SetupChest(Guid.Empty);
        RewardItem rewardItem = AddRewardItem(reward, "SWORD", stackable: false, quantity: 2);
        Inventory inventory = SetupInventory(itemCapacity: 40);

        // Act
        ClaimChestContentsResult result = await CreateHandler()
            .Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Items.Should().HaveCount(2).And.OnlyContain(item => item.Quantity == 1);
        inventory
            .ItemInstances.Select(instance => instance.IdempotencyKey)
            .Should()
            .Equal(
                $"{reward.RewardKey}:{rewardItem.Id}:0",
                $"{reward.RewardKey}:{rewardItem.Id}:1"
            );
        rewardItem.ItemInstanceId.Should().Be(result.Items[0].ItemInstanceId);
    }

    [Fact]
    public async Task Handle_StackableReward_CreatesASingleAvailableStack()
    {
        // Arrange
        RewardEntity reward = SetupChest(Guid.Empty);
        RewardItem rewardItem = AddRewardItem(reward, "POTION", stackable: true, quantity: 5);
        Inventory inventory = SetupInventory(itemCapacity: 40);

        // Act
        ClaimChestContentsResult result = await CreateHandler()
            .Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Items.Should().ContainSingle().Which.Quantity.Should().Be(5);
        ItemInstance itemInstance = inventory.ItemInstances.Should().ContainSingle().Subject;
        itemInstance.ItemId.Should().Be(rewardItem.ItemId);
        itemInstance.InventoryId.Should().Be(inventory.Id);
        itemInstance.Status.Should().Be(ItemInstanceStatus.Available);
        itemInstance.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_EmptyChest_ReturnsEmptyStateWithoutTransferringAnything()
    {
        // Arrange
        Guid firstHeroId = Guid.NewGuid();
        RewardEntity reward = SetupChest(firstHeroId);
        AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);

        // Act
        ClaimChestContentsResult result = await CreateHandler()
            .Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.ChestId.Should().Be(chestId);
        result.State.Should().Be("EMPTY");
        result.Items.Should().BeEmpty();
        result.AlreadyEmpty.Should().BeTrue();
        reward.HeroId.Should().Be(firstHeroId);
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
        chestLootRepository
            .Setup(repository =>
                repository.GetGenerationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ChestLootGeneration?)null);

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
        RewardEntity reward = SetupChest(Guid.Empty);
        AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);
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
    public async Task Handle_UnknownItem_ThrowsNotFoundAndKeepsTheChestFilled()
    {
        // Arrange
        RewardEntity reward = SetupChest(Guid.Empty);
        RewardItem rewardItem = AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        SetupInventory(itemCapacity: 40);
        inventoryRepository
            .Setup(repository =>
                repository.GetItemByIdAsync(rewardItem.ItemId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((Item?)null);

        // Act
        Func<Task> action = () =>
            CreateHandler().Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Item '*");
        reward.HeroId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NotEnoughCapacity_ThrowsWithoutAddingAnyItem()
    {
        // Arrange
        RewardEntity reward = SetupChest(Guid.Empty);
        AddRewardItem(reward, "SWORD", stackable: false, quantity: 2);
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
        new(chestLootRepository.Object, inventoryRepository.Object, new FixedClock());

    private ClaimChestContentsCommand CreateCommand() => new(heroId, dungeonRunId, chestId);

    // Registers the generated chest loot; a reward with a hero is a chest already claimed.
    private RewardEntity SetupChest(Guid rewardHeroId)
    {
        var reward = new RewardEntity
        {
            Id = Guid.NewGuid(),
            HeroId = rewardHeroId,
            RunId = dungeonRunId,
            RewardKey = $"chest:{dungeonRunId:N}:{chestId:N}",
            Status = RewardStatus.Applied,
        };
        var generation = new ChestLootGeneration
        {
            Id = Guid.NewGuid(),
            DungeonRunId = dungeonRunId,
            ChestId = chestId,
            RewardId = reward.Id,
            Reward = reward,
        };
        chestLootRepository
            .Setup(repository =>
                repository.GetGenerationAsync(dungeonRunId, chestId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(generation);
        return reward;
    }

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

    private RewardItem AddRewardItem(
        RewardEntity reward,
        string category,
        bool stackable,
        int quantity
    )
    {
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Stackable = stackable,
            Category = new Category { Label = category },
        };
        var rewardItem = new RewardItem
        {
            Id = Guid.NewGuid(),
            RewardId = reward.Id,
            Reward = reward,
            ItemId = item.Id,
            Item = item,
            Quantity = quantity,
        };
        reward.Items.Add(rewardItem);
        inventoryRepository
            .Setup(repository =>
                repository.GetItemByIdAsync(item.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(item);
        return rewardItem;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = Now;
    }
}
