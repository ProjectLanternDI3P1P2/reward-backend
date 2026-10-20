using MediatR;

namespace Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;

/// <summary>Reads how the group rewards of a dungeon run were distributed.</summary>
public sealed record GetRunRewardDistributionQuery(Guid DungeonRunId)
    : IRequest<RunRewardDistribution>;
