using Reward.Domain.Entities;

namespace Reward.Domain.Repositories;

public interface IMarketplaceRepository
{
    Task<IReadOnlyList<MarketplaceListing>> GetActiveListingsAsync(
        CancellationToken cancellationToken
    );
}
