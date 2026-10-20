using Reward.Application.Abstractions;

namespace Reward.Application.Features.RewardUseCase.GenerateChestLoot;

/// <summary>Requests the immutable loot for one chest in a dungeon run.</summary>
public sealed record GenerateChestLootCommand(
    Guid CommandId,
    Guid DungeonRunId,
    Guid ChestId,
    int Floor,
    string Difficulty
) : ICommand<GenerateChestLootResult>;
