namespace Reward.Domain.Entities;

/// <summary>Records the immutable reward generated for one dungeon chest.</summary>
public sealed class ChestLootGeneration
{
    public Guid Id { get; set; }
    public Guid CommandId { get; set; }
    public Guid DungeonRunId { get; set; }
    public Guid ChestId { get; set; }
    public Guid LootTableId { get; set; }
    public int Floor { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public Guid RewardId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public LootTable LootTable { get; set; } = null!;
    public Reward Reward { get; set; } = null!;
}
