namespace Reward.Application.Ports;

public interface IPlaceholderClient
{
    Task<PlaceholderClientResult?> GetByIdAsync(
        Guid placeholderId,
        CancellationToken cancellationToken
    );
}

public sealed record PlaceholderClientResult(Guid Id, string Name);
