using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;
using System.Data;

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

    public async Task<ActiveEquipmentSnapshot?> GetActiveEquipmentByHeroIdAsync(
        Guid heroId,
        CancellationToken cancellationToken)
    {
        // Both reads share a repeatable-read snapshot so a concurrent equipment change
        // cannot produce slots and modifiers from different points in time.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);

        bool heroExists = await dbContext.Inventories
            .AsNoTracking()
            .AnyAsync(inventory => inventory.HeroId == heroId, cancellationToken);

        if (!heroExists)
        {
            return null;
        }

        List<EquipmentSlot> slots = await dbContext.EquipmentSlots
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<Equipment> equippedItems = await dbContext.Equipment
            .AsNoTracking()
            .Where(equipment => equipment.HeroId == heroId)
            .Include(equipment => equipment.ItemInstance)
                .ThenInclude(itemInstance => itemInstance.Item)
                    .ThenInclude(item => item.Category)
            .Include(equipment => equipment.ItemInstance)
                .ThenInclude(itemInstance => itemInstance.Item)
                    .ThenInclude(item => item.Rarity)
            .Include(equipment => equipment.ItemInstance)
                .ThenInclude(itemInstance => itemInstance.Item)
                    .ThenInclude(item => item.Modifiers)
                        .ThenInclude(itemModifier => itemModifier.Modifier)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, Equipment> equipmentBySlotId = equippedItems.ToDictionary(equipment => equipment.SlotId);

        return new ActiveEquipmentSnapshot(
            heroId,
            slots.Select(slot => new ActiveEquipmentSlotSnapshot(
                slot,
                equipmentBySlotId.GetValueOrDefault(slot.Id))).ToList());
    }
}
