using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;

namespace Reward.Application.Features.InventoryUseCase.GetHeroInventory;

public sealed class GetHeroInventoryQueryHandler(IInventoryRepository repository)
    : IRequestHandler<GetHeroInventoryQuery, HeroInventory?>
{
    public async Task<HeroInventory?> Handle(
        GetHeroInventoryQuery request,
        CancellationToken cancellationToken
    )
    {
        Inventory? inventory = await repository.GetByHeroIdAsync(request.HeroId, cancellationToken);
        if (inventory is null)
        {
            return null;
        }

        List<InventoryItem> items = [];
        List<InventoryItem> consumables = [];

        foreach (
            ItemInstance itemInstance in inventory
                .ItemInstances.OrderBy(itemInstance => itemInstance.Item.Name)
                .ThenBy(itemInstance => itemInstance.Id)
        )
        {
            InventoryItem item = new(
                itemInstance.Id,
                itemInstance.Item.Category.Label,
                itemInstance.Item.Name,
                itemInstance.Item.Rarity.Label,
                itemInstance.Status.ToCode(),
                itemInstance.Quantity,
                itemInstance.Equipment.Any(equipment => equipment.HeroId == request.HeroId),
                itemInstance.Status == ItemInstanceStatus.Reserved
            );

            if (IsConsumable(itemInstance.Item.Category.Label))
            {
                consumables.Add(item);
            }
            else
            {
                items.Add(item);
            }
        }

        return new HeroInventory(inventory.HeroId, items, consumables);
    }

    private static bool IsConsumable(string categoryLabel) =>
        Enum.TryParse(categoryLabel, ignoreCase: true, out ItemCategory category)
        && category.IsConsumable();
}
