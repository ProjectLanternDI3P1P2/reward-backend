namespace Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;

/// <summary>The group rewards of one run, grouped by the event that earned them.</summary>
public sealed record RunRewardDistribution(
    Guid DungeonRunId,
    IReadOnlyList<RewardDistributionCause> Distributions
);

/// <summary>The rewards distributed for one combat, boss or chest of the run.</summary>
public sealed record RewardDistributionCause(
    string SourceType,
    Guid CauseId,
    IReadOnlyList<DistributedReward> Rewards
);

/// <summary>
/// One distributed reward. <paramref name="Kind"/> is UNIQUE (a single recipient) or
/// SHARED_PER_PARTICIPANT (every recipient received their own copy).
/// </summary>
public sealed record DistributedReward(
    string Kind,
    Guid ItemId,
    string Name,
    string Rarity,
    int Quantity,
    IReadOnlyList<RewardRecipient> Recipients
);

public sealed record RewardRecipient(Guid HeroId, Guid? PlayerId, Guid? ItemInstanceId);
