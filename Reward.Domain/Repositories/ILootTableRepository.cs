using Reward.Domain.Entities;

namespace Reward.Domain.Repositories;

public interface ILootTableRepository
{
    /// <summary>
    /// Gets a loot table with its rarity rules, entries and the catalogue items they can
    /// draw, ready to be used by <see cref="Services.LootSelectionService"/>.
    /// </summary>
    Task<LootTable?> GetByIdAsync(Guid lootTableId, CancellationToken cancellationToken);
}
