namespace Reward.Application.Exceptions;

/// <summary>A remote dependency could not be reached or did not answer before its deadline.</summary>
public sealed class ExternalServiceUnavailableException(
    string serviceName,
    Exception? innerException = null
) : Exception($"The {serviceName} service is unavailable.", innerException)
{
    public string ServiceName { get; } = serviceName;
}
