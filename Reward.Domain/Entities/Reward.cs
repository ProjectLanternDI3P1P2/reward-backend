using Reward.Domain.Enums;

namespace Reward.Domain.Entities;

public sealed class Reward
{
    public Guid Id { get; set; }
    public Guid HeroId { get; set; }
    public Guid RunId { get; set; }
    public Guid RewardSourceId { get; set; }
    public RewardType Type { get; set; }
    public RewardStatus Status { get; set; }
    public string RewardKey { get; set; } = string.Empty;
    public int XpAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public RewardSource RewardSource { get; set; } = null!;
    public ICollection<RewardItem> Items { get; set; } = new List<RewardItem>();

    /// <summary>
    /// Identifies one reward operation: the event that caused it and the hero receiving it.
    /// Replaying the same operation always yields the same key.
    /// </summary>
    public static string CreateKey(RewardSourceType sourceType, Guid causeId, Guid heroId) =>
        $"{sourceType.ToCode()}:{causeId}:{heroId}";

    /// <summary>
    /// Identifies a unique group reward: it is keyed by the event and the item, not by the
    /// recipient, so only one participant can ever hold it.
    /// </summary>
    public static string CreateUniqueKey(RewardSourceType sourceType, Guid causeId, Guid itemId) =>
        $"{sourceType.ToCode()}:{causeId}:UNIQUE:{itemId}";

    /// <summary>
    /// Adds a new stack of <paramref name="item"/> to the inventory, within its capacity,
    /// and records it as part of this reward.
    /// </summary>
    public ItemInstance CreditItem(Inventory inventory, Item item, int quantity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(item);

        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            InventoryId = inventory.Id,
            Item = item,
            Inventory = inventory,
            Status = ItemInstanceStatus.Available,
            Quantity = quantity,
            CreatedAt = now,
            UpdatedAt = now,
        };
        inventory.Receive(itemInstance);

        // Preserve the display values recorded when this item was awarded.
        Items.Add(
            new RewardItem
            {
                Id = Guid.NewGuid(),
                RewardId = Id,
                ItemId = item.Id,
                ItemInstanceId = itemInstance.Id,
                ItemNameSnapshot = item.Name,
                ItemRaritySnapshot = item.Rarity?.Label ?? string.Empty,
                Quantity = quantity,
                CreatedAt = now,
            }
        );

        return itemInstance;
    }
}
