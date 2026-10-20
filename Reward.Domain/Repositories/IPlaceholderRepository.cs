using Reward.Domain.Entities;

namespace Reward.Domain.Repositories;

public interface IPlaceholderRepository
{
    Task<Placeholder?> GetByIdAsync(Guid placeholderId, CancellationToken cancellationToken);
}
