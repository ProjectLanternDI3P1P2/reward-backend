using MediatR;
using Reward.Application.Ports;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Application.Features.InventoryUseCase.EquipItem;

public sealed class EquipItemCommandHandler(
    IInventoryRepository repository,
    IHeroProfileClient heroProfileClient,
    IClock clock
) : IRequestHandler<EquipItemCommand>
{
    public async Task Handle(EquipItemCommand request, CancellationToken cancellationToken)
    {
        Inventory inventory =
            await repository.GetByHeroIdForUpdateAsync(request.HeroId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Inventory for hero '{request.HeroId}' was not found."
            );

        ItemInstance itemInstance =
            inventory.ItemInstances.SingleOrDefault(instance =>
                instance.Id == request.ItemInstanceId
            )
            ?? throw new KeyNotFoundException(
                $"Item instance '{request.ItemInstanceId}' was not found."
            );

        if (itemInstance.Status == ItemInstanceStatus.Reserved)
        {
            throw new InvalidOperationException("A reserved item cannot be equipped.");
        }

        if (await repository.HasActiveMarketplaceListingAsync(itemInstance.Id, cancellationToken))
        {
            throw new InvalidOperationException("A listed item cannot be equipped.");
        }

        HeroProfile hero = await heroProfileClient.GetByHeroIdAsync(
            request.HeroId,
            cancellationToken
        );
        ValidateHeroEligibility(itemInstance.Item, hero);

        ItemCategory category = itemInstance.Item.Category.Label.ToItemCategory();
        IReadOnlyList<EquipmentSlotName> compatibleSlotNames = GetCompatibleSlotNames(category);
        IReadOnlyList<EquipmentSlot> compatibleSlots =
            await repository.GetEquipmentSlotsByNamesAsync(
                compatibleSlotNames.Select(slot => slot.ToCode()).ToList(),
                cancellationToken
            );
        if (compatibleSlots.Count != compatibleSlotNames.Count)
        {
            throw new InvalidOperationException("The required equipment slots are not configured.");
        }

        EquipmentSlot requestedSlot =
            compatibleSlots.SingleOrDefault(slot => slot.Id == request.SlotId)
            ?? throw new InvalidOperationException(
                "The selected slot is not compatible with this item."
            );
        IReadOnlyList<EquipmentSlot> targetSlots = IsTwoHanded(category)
            ? compatibleSlots
            : [requestedSlot];

        DateTimeOffset now = clock.UtcNow;
        HashSet<Guid> requiredSlotIds = targetSlots.Select(slot => slot.Id).ToHashSet();
        List<ItemInstance> displacedItems = inventory
            .ItemInstances.Where(instance =>
                instance.Id != itemInstance.Id
                && instance.Equipment.Any(equipment => requiredSlotIds.Contains(equipment.SlotId))
            )
            .ToList();

        foreach (ItemInstance displacedItem in displacedItems)
        {
            foreach (Equipment equipment in displacedItem.Equipment.ToList())
            {
                repository.RemoveEquipment(equipment);
            }

            displacedItem.Status = ItemInstanceStatus.Available;
            displacedItem.UpdatedAt = now;
        }

        foreach (Equipment equipment in itemInstance.Equipment.ToList())
        {
            repository.RemoveEquipment(equipment);
        }

        foreach (EquipmentSlot slot in targetSlots)
        {
            repository.AddEquipment(
                new Equipment
                {
                    Id = Guid.NewGuid(),
                    HeroId = request.HeroId,
                    ItemInstanceId = itemInstance.Id,
                    SlotId = slot.Id,
                    Quantity = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        itemInstance.Status = ItemInstanceStatus.Equipped;
        itemInstance.UpdatedAt = now;
    }

    private static void ValidateHeroEligibility(Item item, HeroProfile hero)
    {
        if (hero.Level < item.LevelRequired)
        {
            throw new InvalidOperationException("The hero level is too low to equip this item.");
        }

        IReadOnlyList<string> requiredClasses = item
            .ClassTags.Select(tag => tag.ClassTag.Label)
            .ToList();
        if (requiredClasses.Count > 0 && !requiredClasses.Any(hero.ClassTags.Contains))
        {
            throw new InvalidOperationException("The hero class cannot equip this item.");
        }
    }

    private static IReadOnlyList<EquipmentSlotName> GetCompatibleSlotNames(ItemCategory category) =>
        category switch
        {
            ItemCategory.TwoHandedSword =>
            [
                EquipmentSlotName.RightHand,
                EquipmentSlotName.LeftHand,
            ],
            ItemCategory.Sword or ItemCategory.Weapon => [EquipmentSlotName.RightHand],
            ItemCategory.Shield => [EquipmentSlotName.LeftHand],
            ItemCategory.Armor => [EquipmentSlotName.Body],
            ItemCategory.Ring or ItemCategory.Amulet =>
            [
                EquipmentSlotName.Jewelry1,
                EquipmentSlotName.Jewelry2,
            ],
            _ => throw new InvalidOperationException("This item category cannot be equipped."),
        };

    private static bool IsTwoHanded(ItemCategory category) =>
        category == ItemCategory.TwoHandedSword;
}
