using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Application.Features.InventoryUseCase.GetActiveEquipment;

public sealed class GetActiveEquipmentQueryHandler(IInventoryRepository repository)
    : IRequestHandler<GetActiveEquipmentQuery, ActiveEquipment?>
{
    public async Task<ActiveEquipment?> Handle(
        GetActiveEquipmentQuery request,
        CancellationToken cancellationToken
    )
    {
        ActiveEquipmentSnapshot? snapshot = await repository.GetActiveEquipmentByHeroIdAsync(
            request.HeroId,
            cancellationToken
        );

        if (snapshot is null)
        {
            return null;
        }

        return new ActiveEquipment(
            snapshot.HeroId,
            snapshot
                .Slots.OrderBy(slot => slot.Slot.Name)
                .ThenBy(slot => slot.Slot.Id)
                .Select(slot => new ActiveEquipmentSlot(
                    slot.Slot.Id,
                    slot.Slot.Name,
                    slot.Equipment is null ? null : MapEquippedItem(slot.Equipment)
                ))
                .ToList()
        );
    }

    private static EquippedItem MapEquippedItem(Equipment equipment)
    {
        Item item = equipment.ItemInstance.Item;
        return new EquippedItem(
            equipment.ItemInstance.Id,
            item.Id,
            item.Category.Label,
            item.Name,
            item.Rarity.Label,
            item.Modifiers.Select(itemModifier => itemModifier.Modifier)
                .OrderBy(modifier => modifier.Stat)
                .ThenBy(modifier => modifier.Name)
                .ThenBy(modifier => modifier.Id)
                .Select(modifier => new CombatModifier(
                    modifier.Name,
                    modifier.Stat,
                    modifier.Value,
                    modifier.Type
                ))
                .ToList()
        );
    }
}
