namespace Reward.Domain.Exceptions;

/// <summary>The hero, the item or the selected slot does not allow this equipment.</summary>
public sealed class ItemNotEquippableException(string reason) : DomainException(reason);
