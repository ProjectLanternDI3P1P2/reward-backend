using FluentAssertions;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Domain;

public sealed class ChestTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void State_RewardWithoutHero_IsFilled()
    {
        // Arrange
        var chest = new Chest(Guid.NewGuid(), CreateReward(Guid.Empty));

        // Act
        ChestState state = chest.State;

        // Assert
        state.Should().Be(ChestState.Filled);
    }

    [Fact]
    public void State_RewardClaimedByAHero_IsEmpty()
    {
        // Arrange
        var chest = new Chest(Guid.NewGuid(), CreateReward(Guid.NewGuid()));

        // Act
        ChestState state = chest.State;

        // Assert
        state.Should().Be(ChestState.Empty);
    }

    [Fact]
    public void TransferTo_NonStackableReward_CreatesOneItemInstancePerUnit()
    {
        // Arrange
        RewardEntity reward = CreateReward(Guid.Empty);
        RewardItem rewardItem = AddRewardItem(reward, "SWORD", stackable: false, quantity: 2);
        var chest = new Chest(Guid.NewGuid(), reward);
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
        reward.HeroId.Should().Be(inventory.HeroId);
        chest.State.Should().Be(ChestState.Empty);
    }

    [Fact]
    public void TransferTo_StackableReward_CreatesASingleAvailableStack()
    {
        // Arrange
        RewardEntity reward = CreateReward(Guid.Empty);
        RewardItem rewardItem = AddRewardItem(reward, "POTION", stackable: true, quantity: 5);
        var chest = new Chest(Guid.NewGuid(), reward);
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
    }

    [Fact]
    public void TransferTo_EmptyChest_TransfersNothing()
    {
        // Arrange
        Guid firstHeroId = Guid.NewGuid();
        RewardEntity reward = CreateReward(firstHeroId);
        RewardItem rewardItem = AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        var chest = new Chest(Guid.NewGuid(), reward);
        Inventory inventory = CreateInventory(itemCapacity: 40);

        // Act
        IReadOnlyList<ItemInstance> itemInstances = chest.TransferTo(inventory, Now);

        // Assert
        itemInstances.Should().BeEmpty();
        inventory.ItemInstances.Should().BeEmpty();
        rewardItem.ItemInstanceId.Should().BeNull();
        reward.HeroId.Should().Be(firstHeroId);
    }

    [Fact]
    public void TransferTo_NotEnoughCapacity_ThrowsAndKeepsTheChestFilled()
    {
        // Arrange
        RewardEntity reward = CreateReward(Guid.Empty);
        AddRewardItem(reward, "SWORD", stackable: false, quantity: 1);
        var chest = new Chest(Guid.NewGuid(), reward);
        Inventory inventory = CreateInventory(itemCapacity: 0);

        // Act
        Action action = () => chest.TransferTo(inventory, Now);

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("*item capacity of 0*");
        chest.State.Should().Be(ChestState.Filled);
    }

    private static RewardEntity CreateReward(Guid heroId) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            RewardKey = $"chest:{Guid.NewGuid():N}:{Guid.NewGuid():N}",
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

    private static Inventory CreateInventory(int itemCapacity) =>
        new()
        {
            Id = Guid.NewGuid(),
            HeroId = Guid.NewGuid(),
            ItemCapacity = itemCapacity,
            PotionCapacity = 20,
        };
}
