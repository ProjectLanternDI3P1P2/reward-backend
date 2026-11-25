namespace Reward.Domain.Exceptions;

/// <summary>
/// The item instance is committed elsewhere (reserved for a trade, listed on the marketplace)
/// and cannot be used until it is released.
/// </summary>
public sealed class ItemUnavailableException(string reason) : DomainException(reason);
