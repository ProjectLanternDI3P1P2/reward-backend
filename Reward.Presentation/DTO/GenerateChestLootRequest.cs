using System.Text.Json.Serialization;

namespace Reward.Presentation.DTO;

/// <summary>Identifies one chest interaction in a dungeon run.</summary>
public sealed record GenerateChestLootRequest(
    Guid CommandId,
    Guid DungeonRunId,
    [property: JsonRequired] int ChestElementId,
    int Floor,
    string Difficulty
);
