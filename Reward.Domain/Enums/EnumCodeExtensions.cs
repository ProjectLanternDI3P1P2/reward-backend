namespace Reward.Domain.Enums;

public static class EnumCodeExtensions
{
    public static string ToCode<TEnum>(this TEnum value)
        where TEnum : struct, Enum => value.ToString().ToUpperInvariant();

    public static bool IsConsumable(this ItemCategory category) =>
        category is ItemCategory.Potion or ItemCategory.Vial;

    public static string ToCode(this EquipmentSlotName slot) =>
        slot switch
        {
            EquipmentSlotName.RightHand => "RIGHT_HAND",
            EquipmentSlotName.LeftHand => "LEFT_HAND",
            EquipmentSlotName.Body => "BODY",
            EquipmentSlotName.Jewelry1 => "JEWELRY_1",
            EquipmentSlotName.Jewelry2 => "JEWELRY_2",
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null),
        };
}
