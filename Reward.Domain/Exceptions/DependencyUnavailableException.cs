namespace Reward.Domain.Exceptions;

/// <summary>
/// A service Reward depends on could not answer in time. Presentation maps it to HTTP 503:
/// the request may succeed later and nothing protected is exposed meanwhile.
/// </summary>
public sealed class DependencyUnavailableException(string dependency, Exception innerException)
    : Exception($"The {dependency} service is unavailable.", innerException);
