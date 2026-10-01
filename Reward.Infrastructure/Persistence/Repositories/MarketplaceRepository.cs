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
            .Where(listing => listing.Status == "ACTIVE")
            .Include(listing => listing.ItemInstance)
                .ThenInclude(itemInstance => itemInstance.Item)
                    .ThenInclude(item => item.Category)
            .Include(listing => listing.ItemInstance)
                .ThenInclude(itemInstance => itemInstance.Item)
                    .ThenInclude(item => item.Rarity)
            .OrderBy(listing => listing.CreatedAt)
            .ToListAsync(cancellationToken);
}
