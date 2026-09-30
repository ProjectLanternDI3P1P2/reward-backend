namespace Reward.Domain.Entities;

public sealed class LootTable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Floor { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public int DrawCount { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<LootRarityRule> RarityRules { get; set; } = new List<LootRarityRule>();
}
