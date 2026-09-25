namespace Reward.Domain.Entities;

public sealed class LootRarityRule
{
    public Guid Id { get; set; }
    public Guid LootTableId { get; set; }
    public Guid RarityId { get; set; }
    public int Weight { get; set; }
    public LootTable LootTable { get; set; } = null!;
    public Rarity Rarity { get; set; } = null!;
}
