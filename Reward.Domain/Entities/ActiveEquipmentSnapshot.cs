namespace Reward.Domain.Entities;

/// <summary>Immutable view of the equipment that contributes to a hero's combat statistics.</summary>
public sealed record ActiveEquipmentSnapshot(
    Guid HeroId,
    IReadOnlyList<ActiveEquipmentSlotSnapshot> Slots
);

public sealed record ActiveEquipmentSlotSnapshot(EquipmentSlot Slot, Equipment? Equipment);
