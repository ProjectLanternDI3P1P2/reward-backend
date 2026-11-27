namespace Reward.Domain.Enums;

/// <summary>How a reward is distributed among the participants of an activity.</summary>
public enum RewardDistribution
{
    /// <summary>Claimed by a single hero, outside any group distribution (e.g. a chest).</summary>
    Individual,

    /// <summary>One copy awarded to exactly one eligible participant.</summary>
    Unique,

    /// <summary>The same reward credited to every eligible participant.</summary>
    SharedPerParticipant,
}
