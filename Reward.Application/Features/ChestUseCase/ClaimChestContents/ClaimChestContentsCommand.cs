using Reward.Application.Abstractions;

namespace Reward.Application.Features.ChestUseCase.ClaimChestContents;

public sealed record ClaimChestContentsCommand(Guid HeroId, Guid DungeonRunId, Guid ChestId)
    : ICommand<ClaimChestContentsResult>;
