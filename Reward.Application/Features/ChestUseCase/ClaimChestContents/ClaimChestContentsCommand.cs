using Reward.Application.Abstractions;

namespace Reward.Application.Features.ChestUseCase.ClaimChestContents;

public sealed record ClaimChestContentsCommand(Guid HeroId, Guid ChestId)
    : ICommand<ClaimChestContentsResult>;
