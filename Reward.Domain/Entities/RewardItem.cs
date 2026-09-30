namespace Reward.Domain.Entities;

/// <summary>Stores one aggregated reward item with immutable display snapshots.</summary>
public sealed class RewardItem
{
    public Guid Id { get; set; }
    public Guid RewardId { get; set; }
    public Guid ItemId { get; set; }
    public Guid? ItemInstanceId { get; set; }
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public string ItemRaritySnapshot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Reward Reward { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public ItemInstance? ItemInstance { get; set; }
}
