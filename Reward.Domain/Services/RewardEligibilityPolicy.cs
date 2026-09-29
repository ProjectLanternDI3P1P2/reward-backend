using Reward.Domain.Enums;
using Reward.Domain.ValueObjects;

namespace Reward.Domain.Services;

/// <summary>
/// Decides which players of a completed group activity may receive its rewards.
/// Only registered participants qualify. Dying or disconnecting keeps a player
/// part of the activity; leaving voluntarily forfeits the rewards.
/// </summary>
public static class RewardEligibilityPolicy
{
    public static IReadOnlyList<Guid> GetEligiblePlayerIds(
        IReadOnlyCollection<ActivityParticipant> participants
    )
    {
        ArgumentNullException.ThrowIfNull(participants);
        EnsureEachPlayerIsRegisteredOnce(participants);

        return participants
            .Where(participant => IsEligible(participant.Status))
            .Select(participant => participant.PlayerId)
            .ToList();
    }

    public static bool IsEligible(
        Guid playerId,
        IReadOnlyCollection<ActivityParticipant> participants
    ) => GetEligiblePlayerIds(participants).Contains(playerId);

    // Statuses are listed explicitly so that a future status is not eligible by default.
    private static bool IsEligible(ParticipationStatus status) =>
        status
            is ParticipationStatus.Active
                or ParticipationStatus.Dead
                or ParticipationStatus.Disconnected;

    private static void EnsureEachPlayerIsRegisteredOnce(
        IReadOnlyCollection<ActivityParticipant> participants
    )
    {
        bool hasDuplicate =
            participants.Select(participant => participant.PlayerId).Distinct().Count()
            != participants.Count;
        if (hasDuplicate)
        {
            throw new ArgumentException(
                "A player cannot be registered more than once in an activity.",
                nameof(participants)
            );
        }
    }
}
