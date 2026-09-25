using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository(RewardDbContext dbContext) : IInventoryRepository
{
    public Task<Inventory?> GetByHeroIdAsync(Guid heroId, CancellationToken cancellationToken) =>
        dbContext.Inventories
            .AsNoTracking()
            .Include(inventory => inventory.ItemInstances)
                .ThenInclude(itemInstance => itemInstance.Item)
                    .ThenInclude(item => item.Category)
            .Include(inventory => inventory.ItemInstances)
                .ThenInclude(itemInstance => itemInstance.Item)
                    .ThenInclude(item => item.Rarity)
            .Include(inventory => inventory.ItemInstances)
                .ThenInclude(itemInstance => itemInstance.Equipment)
            .SingleOrDefaultAsync(inventory => inventory.HeroId == heroId, cancellationToken);
}
