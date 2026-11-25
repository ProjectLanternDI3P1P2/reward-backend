using Reward.Domain.Exceptions;
using Reward.Domain.ValueObjects;

namespace Reward.Domain.Services;

/// <summary>
/// Draws the single participant who receives a unique group reward.
/// Every eligible participant has the same chance; ineligible ones never win.
/// </summary>
public static class UniqueRewardRecipientSelector
{
    public static ActivityParticipant SelectRecipient(
        IReadOnlyCollection<ActivityParticipant> participants,
        IRandomSource random
    )
    {
        ArgumentNullException.ThrowIfNull(random);

        IReadOnlyList<ActivityParticipant> eligible =
            RewardEligibilityPolicy.GetEligibleParticipants(participants);
        if (eligible.Count == 0)
        {
            throw new NoEligibleRecipientException();
        }

        return eligible[random.NextInt32(eligible.Count)];
    }
}
