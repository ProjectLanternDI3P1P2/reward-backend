namespace Reward.Domain.Exceptions;

/// <summary>The item instance cannot be consumed: not a consumable, or nothing left of it.</summary>
public sealed class ItemNotConsumableException(string reason) : DomainException(reason);
