using Reward.Domain.Enums;

namespace Reward.Domain.Entities;

/// <summary>
/// The rewards generated for a dungeon chest. The chest itself belongs to another
/// microservice; this service only stores its contents as a <see cref="Reward"/>
/// whose key is derived from the chest identifier.
/// </summary>
public sealed class Chest
{
    private const string RewardKeyPrefix = "CHEST:";

    private readonly Reward reward;
    private readonly IReadOnlyList<RewardItem> rewardItems;

    public Chest(Guid chestId, Reward reward, IReadOnlyList<RewardItem> rewardItems)
    {
        ArgumentNullException.ThrowIfNull(reward);
        ArgumentNullException.ThrowIfNull(rewardItems);

        ChestId = chestId;
        this.reward = reward;
        this.rewardItems = rewardItems;
    }

    public Guid ChestId { get; }

    // A completed reward has already been transferred, so the chest has nothing left.
    public ChestState State =>
        reward.Status == RewardStatus.Completed.ToCode() ? ChestState.Empty : ChestState.Filled;

    public static string CreateRewardKey(Guid chestId) => $"{RewardKeyPrefix}{chestId}";

    /// <summary>
    /// Moves every reward of the chest into the inventory and empties the chest.
    /// The inventory rejects the first reward that exceeds its capacity; the caller's
    /// transaction must then be rolled back so the chest keeps all its rewards.
    /// </summary>
    public IReadOnlyList<ItemInstance> TransferTo(Inventory inventory, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        if (State == ChestState.Empty)
        {
            return [];
        }

        var transferredItems = new List<ItemInstance>();
        foreach (RewardItem rewardItem in rewardItems)
        {
            IReadOnlyList<ItemInstance> itemInstances = CreateItemInstances(
                rewardItem,
                inventory,
                now
            );
            foreach (ItemInstance itemInstance in itemInstances)
            {
                inventory.Receive(itemInstance);
            }

            rewardItem.ItemInstanceId = itemInstances[0].Id;
            rewardItem.ItemInstance = itemInstances[0];
            transferredItems.AddRange(itemInstances);
        }

        reward.Status = RewardStatus.Completed.ToCode();
        return transferredItems;
    }

    // A stackable reward becomes one stack; any other reward takes one inventory
    // slot per unit, as items added one by one do.
    private IReadOnlyList<ItemInstance> CreateItemInstances(
        RewardItem rewardItem,
        Inventory inventory,
        DateTimeOffset now
    )
    {
        if (rewardItem.Item.Stackable)
        {
            return [CreateItemInstance(rewardItem, inventory, rewardItem.Quantity, 0, now)];
        }

        return Enumerable
            .Range(0, rewardItem.Quantity)
            .Select(unit => CreateItemInstance(rewardItem, inventory, 1, unit, now))
            .ToList();
    }

    // The idempotency key is unique per unit, so the database also refuses a second
    // transfer of the same reward item.
    private ItemInstance CreateItemInstance(
        RewardItem rewardItem,
        Inventory inventory,
        int quantity,
        int unit,
        DateTimeOffset now
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ItemId = rewardItem.ItemId,
            InventoryId = inventory.Id,
            Item = rewardItem.Item,
            Inventory = inventory,
            Status = ItemInstanceStatus.Available,
            Quantity = quantity,
            IdempotencyKey = $"{reward.RewardKey}:{rewardItem.Id}:{unit}",
            CreatedAt = now,
            UpdatedAt = now,
        };
}
