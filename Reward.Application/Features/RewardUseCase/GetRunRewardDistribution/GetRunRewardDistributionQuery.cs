using MediatR;

namespace Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;

/// <summary>Reads how the group rewards of a dungeon run were distributed.</summary>
/// <param name="ExternalUserId">Trusted gateway identity of the caller (X-User-Id).</param>
public sealed record GetRunRewardDistributionQuery(Guid DungeonRunId, string? ExternalUserId)
    : IRequest<RunRewardDistribution>;
