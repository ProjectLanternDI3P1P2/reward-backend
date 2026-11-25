namespace Reward.Domain.Exceptions;

/// <summary>
/// The request carries no trusted user identity although the resource belongs to players.
/// Presentation maps it to HTTP 401.
/// </summary>
public sealed class UnauthenticatedException(string message) : Exception(message);
