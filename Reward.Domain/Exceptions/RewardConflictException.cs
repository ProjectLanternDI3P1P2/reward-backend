namespace Reward.Domain.Exceptions;

/// <summary>
/// A replayed reward operation does not match the reward already recorded under its key:
/// granting it again would change what the hero already received.
/// </summary>
public sealed class RewardConflictException(string rewardKey, string reason)
    : DomainException($"Reward '{rewardKey}' {reason}.");
