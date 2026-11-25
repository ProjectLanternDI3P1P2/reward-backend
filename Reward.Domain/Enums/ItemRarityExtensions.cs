namespace Reward.Domain.Enums;

public static class ItemRarityExtensions
{
    /// <summary>
    /// The rank the rarity catalogue stores for a default rarity, from 1 for Common to 5 for
    /// Legendary. It is spelled out so that reordering the enum cannot change a rank.
    /// </summary>
    public static int ToRank(this ItemRarity rarity) =>
        rarity switch
        {
            ItemRarity.Common => 1,
            ItemRarity.Uncommon => 2,
            ItemRarity.Rare => 3,
            ItemRarity.Epic => 4,
            ItemRarity.Legendary => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null),
        };
}
