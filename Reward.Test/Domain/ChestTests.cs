using FluentAssertions;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Domain;

public sealed class ChestTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateRewardKey_ChestId_PrefixesTheIdentifier()
    {
        // Arrange
        var chestId = Guid.Parse("7d1f0d4c-4f8e-4b0a-9f3e-2f1b8c9a6d10");

        // Act
        string rewardKey = Chest.CreateRewardKey(chestId);

        // Assert
        rewardKey.Should().Be("CHEST:7d1f0d4c-4f8e-4b0a-9f3e-2f1b8c9a6d10");
    }

    [Theory]
    [InlineData(RewardStatus.Pending, ChestState.Filled)]
    [InlineData(RewardStatus.Failed, ChestState.Filled)]
    [InlineData(RewardStatus.Completed, ChestState.Empty)]
    public void State_RewardStatus_ReflectsWhetherRewardsRemain(
        RewardStatus rewardStatus,
        ChestState expectedState
    )
    {
        // Arrange
        var chest = new Chest(Guid.NewGuid(), CreateReward(rewardStatus), []);

        // Act
        ChestState state = chest.State;

        // Assert
        state.Should().Be(expectedState);
    }

    [Fact]
    public void TransferTo_NonStackableReward_CreatesOneItemInstancePerUnit()
    {
        // Arrange
        RewardEntity reward = CreateReward(RewardStatus.Pending);
        RewardItem rewardItem = CreateRewardItem(reward, "SWORD", stackable: false, quantity: 2);
        var chest = new Chest(Guid.NewGuid(), reward, [rewardItem]);
        Inventory inventory = CreateInventory(itemCapacity: 40);

        // Act
        IReadOnlyList<ItemInstance> itemInstances = chest.TransferTo(inventory, Now);

        // Assert
        itemInstances.Should().HaveCount(2).And.OnlyContain(instance => instance.Quantity == 1);
        itemInstances
            .Select(instance => instance.IdempotencyKey)
            .Should()
            .Equal(
                $"{reward.RewardKey}:{rewardItem.Id}:0",
                $"{reward.RewardKey}:{rewardItem.Id}:1"
            );
        inventory.ItemInstances.Should().BeEquivalentTo(itemInstances);
        rewardItem.ItemInstanceId.Should().Be(itemInstances[0].Id);
        chest.State.Should().Be(ChestState.Empty);
    }

    [Fact]
    public void TransferTo_StackableReward_CreatesASingleAvailableStack()
    {
        // Arrange
        RewardEntity reward = CreateReward(RewardStatus.Pending);
        RewardItem rewardItem = CreateRewardItem(reward, "POTION", stackable: true, quantity: 5);
        var chest = new Chest(Guid.NewGuid(), reward, [rewardItem]);
        Inventory inventory = CreateInventory(itemCapacity: 40);

        // Act
        IReadOnlyList<ItemInstance> itemInstances = chest.TransferTo(inventory, Now);

        // Assert
        ItemInstance itemInstance = itemInstances.Should().ContainSingle().Subject;
        itemInstance.ItemId.Should().Be(rewardItem.ItemId);
        itemInstance.InventoryId.Should().Be(inventory.Id);
        itemInstance.Status.Should().Be(ItemInstanceStatus.Available);
        itemInstance.Quantity.Should().Be(5);
        itemInstance.CreatedAt.Should().Be(Now);
        reward.Status.Should().Be("COMPLETED");
    }

    [Fact]
    public void TransferTo_EmptyChest_TransfersNothing()
    {
        // Arrange
        RewardEntity reward = CreateReward(RewardStatus.Completed);
        RewardItem rewardItem = CreateRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        var chest = new Chest(Guid.NewGuid(), reward, [rewardItem]);
        Inventory inventory = CreateInventory(itemCapacity: 40);

        // Act
        IReadOnlyList<ItemInstance> itemInstances = chest.TransferTo(inventory, Now);

        // Assert
        itemInstances.Should().BeEmpty();
        inventory.ItemInstances.Should().BeEmpty();
        rewardItem.ItemInstanceId.Should().BeNull();
    }

    [Fact]
    public void TransferTo_NotEnoughCapacity_ThrowsAndKeepsTheChestFilled()
    {
        // Arrange
        RewardEntity reward = CreateReward(RewardStatus.Pending);
        RewardItem rewardItem = CreateRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        var chest = new Chest(Guid.NewGuid(), reward, [rewardItem]);
        Inventory inventory = CreateInventory(itemCapacity: 0);

        // Act
        Action action = () => chest.TransferTo(inventory, Now);

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("*item capacity of 0*");
        chest.State.Should().Be(ChestState.Filled);
    }

    private static RewardEntity CreateReward(RewardStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            RewardKey = Chest.CreateRewardKey(Guid.NewGuid()),
            Status = status.ToCode(),
        };

    private static RewardItem CreateRewardItem(
        RewardEntity reward,
        string category,
        bool stackable,
        int quantity
    )
    {
        Guid itemId = Guid.NewGuid();
        return new RewardItem
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
    }

    private static Inventory CreateInventory(int itemCapacity) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = Guid.NewGuid(),
            ItemCapacity = itemCapacity,
            PotionCapacity = 20,
        };
}
