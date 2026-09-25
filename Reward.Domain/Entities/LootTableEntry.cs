namespace Reward.Domain.Entities;

public sealed class LootTableEntry
{
    public Guid Id { get; set; }
    public Guid LootRarityRuleId { get; set; }
    public Guid ItemId { get; set; }
    public int Weight { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public LootRarityRule LootRarityRule { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
