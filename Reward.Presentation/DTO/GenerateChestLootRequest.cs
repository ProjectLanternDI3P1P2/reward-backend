namespace Reward.Presentation.DTO;

/// <summary>Identifies one chest interaction in a dungeon run.</summary>
public sealed record GenerateChestLootRequest
{
    public required Guid CommandId { get; init; }
    public required Guid DungeonRunId { get; init; }
    public required Guid ChestId { get; init; }
    public required int Floor { get; init; }
    public required string Difficulty { get; init; }
}
