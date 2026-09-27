namespace Reward.Domain.Enums;

public static class EnumCodeExtensions
{
    public static string ToCode<TEnum>(this TEnum value)
        where TEnum : struct, Enum => value.ToString().ToUpperInvariant();

    public static string ToCode(this ItemCategory category) =>
        category switch
        {
            ItemCategory.Weapon => "WEAPON",
            ItemCategory.Sword => "SWORD",
            ItemCategory.TwoHandedSword => "TWO_HANDED_SWORD",
            ItemCategory.Armor => "ARMOR",
            ItemCategory.Shield => "SHIELD",
            ItemCategory.Ring => "RING",
            ItemCategory.Amulet => "AMULET",
            ItemCategory.Potion => "POTION",
            ItemCategory.Vial => "VIAL",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

    public static ItemCategory ToItemCategory(this string code) =>
        code.ToUpperInvariant() switch
        {
            "WEAPON" => ItemCategory.Weapon,
            "SWORD" => ItemCategory.Sword,
            "TWO_HANDED_SWORD" => ItemCategory.TwoHandedSword,
            "ARMOR" => ItemCategory.Armor,
            "SHIELD" => ItemCategory.Shield,
            "RING" => ItemCategory.Ring,
            "AMULET" => ItemCategory.Amulet,
            "POTION" => ItemCategory.Potion,
            "VIAL" => ItemCategory.Vial,
            _ => throw new InvalidOperationException($"Unknown item category '{code}'."),
        };

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
