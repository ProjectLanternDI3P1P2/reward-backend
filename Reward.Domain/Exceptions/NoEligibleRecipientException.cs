namespace Reward.Domain.Exceptions;

/// <summary>No participant of the activity is eligible to receive the unique reward.</summary>
public sealed class NoEligibleRecipientException()
    : DomainException("No eligible participant can receive the unique reward.");
