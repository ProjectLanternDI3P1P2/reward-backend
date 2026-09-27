namespace Reward.Domain.Enums;

/// <summary>
/// Known item categories that have dedicated inventory rules.
/// Other categories may still be managed from the catalogue.
/// </summary>
public enum ItemCategory
{
    Weapon,
    Sword,
    TwoHandedSword,
    Armor,
    Shield,
    Ring,
    Amulet,
    Potion,
    Vial,
}
