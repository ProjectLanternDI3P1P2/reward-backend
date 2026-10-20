namespace Reward.Application.Features.RewardUseCase.GenerateChestLoot;

/// <summary>Returns a generated or replayed immutable chest reward.</summary>
public sealed record GenerateChestLootResult(
    Guid RewardId,
    Guid DungeonRunId,
    Guid ChestId,
    Guid LootTableId,
    int Floor,
    string Difficulty,
    bool AlreadyGenerated,
    IReadOnlyList<GeneratedChestLootItem> Items
);

/// <summary>Describes one aggregated catalogue item in a chest reward.</summary>
public sealed record GeneratedChestLootItem(Guid ItemId, string Name, string Rarity, int Quantity);
