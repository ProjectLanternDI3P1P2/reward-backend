namespace Reward.Application.Features.InventoryUseCase.GetActiveEquipment;

public sealed record ActiveEquipment(Guid HeroId, IReadOnlyList<ActiveEquipmentSlot> Slots);

public sealed record ActiveEquipmentSlot(Guid SlotId, string SlotName, EquippedItem? EquippedItem);

public sealed record EquippedItem(
    Guid ItemInstanceId,
    Guid ItemId,
    string Type,
    string Name,
    string Rarity,
    IReadOnlyList<CombatModifier> Modifiers
);

public sealed record CombatModifier(string Name, string Stat, decimal Value, string Type);
