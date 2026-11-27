namespace Reward.Domain.Exceptions;

/// <summary>
/// A business rule refused the operation in the current state. Presentation maps it to
/// HTTP 409 and gRPC FAILED_PRECONDITION, as opposed to input validation failures (422) and
/// to server or configuration failures (500), which keep their own exception types.
/// </summary>
public abstract class DomainException(string message) : Exception(message);
