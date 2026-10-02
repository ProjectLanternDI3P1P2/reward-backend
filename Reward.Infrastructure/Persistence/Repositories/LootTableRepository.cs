using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Infrastructure.Persistence.Repositories;

public sealed class LootTableRepository(RewardDbContext dbContext) : ILootTableRepository
{
    public Task<LootTable?> GetByIdAsync(Guid lootTableId, CancellationToken cancellationToken) =>
        dbContext
            .LootTables.AsSplitQuery()
            .Include(table => table.RarityRules)
                .ThenInclude(rule => rule.Entries)
                    .ThenInclude(entry => entry.Item)
                        .ThenInclude(item => item.Rarity)
            // The inventory checks the category of every item it receives.
            .Include(table => table.RarityRules)
                .ThenInclude(rule => rule.Entries)
                    .ThenInclude(entry => entry.Item)
                        .ThenInclude(item => item.Category)
            .SingleOrDefaultAsync(table => table.Id == lootTableId, cancellationToken);
}
