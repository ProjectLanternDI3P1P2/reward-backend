using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;
using Reward.Infrastructure.Persistence;

namespace Reward.Infrastructure.Persistence.Repositories;

public sealed class MarketplaceRepository(RewardDbContext dbContext) : IMarketplaceRepository
{
    public async Task<IReadOnlyList<MarketplaceListing>> GetActiveListingsAsync(
        CancellationToken cancellationToken
    ) =>
        await dbContext
            .MarketplaceListings.AsNoTracking()
            .Where(listing => listing.Status == MarketplaceListingStatus.Active)
            .Include(listing => listing.ItemInstance.Item.Category)
            .Include(listing => listing.ItemInstance.Item.Rarity)
            .OrderBy(listing => listing.CreatedAt)
            .ToListAsync(cancellationToken);
}
