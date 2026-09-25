using Reward.Domain.Entities;

namespace Reward.Domain.Repositories;

public interface IInventoryRepository
{
    Task<Inventory?> GetByHeroIdAsync(Guid heroId, CancellationToken cancellationToken);
}
