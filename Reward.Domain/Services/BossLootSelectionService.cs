using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Exceptions;

namespace Reward.Domain.Services;

/// <summary>
/// Selects the loot of a boss: the regular draws of its loot table, plus one equipment item
/// of Rare rarity or higher when those draws did not already yield one.
/// </summary>
public sealed class BossLootSelectionService(LootSelectionService lootSelection)
{
    // Ranks follow the rarity catalogue: 1 = Common, 2 = Uncommon, 3 = Rare, and so on.
    private static readonly int MinimumGuaranteedRank = (int)ItemRarity.Rare + 1;

    /// <summary>Draws the boss loot and guarantees one high-rarity equipment item in it.</summary>
    public IReadOnlyList<SelectedLootItem> Select(LootTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        // Reject a table that cannot honour the guarantee before consuming any randomness.
        LootTable guaranteedTable = CreateGuaranteedTable(table);

        IReadOnlyList<SelectedLootItem> drawn = lootSelection.Select(table);
        if (drawn.Any(selected => IsGuaranteedLoot(selected.Item)))
        {
            return drawn;
        }

        // Draw the missing item with the table's own weights, restricted to qualifying entries.
        // The drawn items cannot contain it, so it is added without any aggregation.
        SelectedLootItem guaranteed = lootSelection.Select(guaranteedTable).Single();
        return drawn.Append(guaranteed).OrderBy(selected => selected.Item.Id).ToList();
    }

    // Builds a throwaway single-draw view of the table that keeps only the qualifying entries.
    // It reuses the table's weights and quantity ranges and is never attached to the database.
    private static LootTable CreateGuaranteedTable(LootTable table)
    {
        var guaranteedTable = new LootTable
        {
            Id = table.Id,
            Name = table.Name,
            Floor = table.Floor,
            Difficulty = table.Difficulty,
            SourceType = table.SourceType,
            DrawCount = 1,
        };

        foreach (LootRarityRule rule in table.RarityRules)
        {
            List<LootTableEntry> entries = rule
                .Entries.Where(entry => entry.Item is not null && IsGuaranteedLoot(entry.Item))
                .ToList();
            if (entries.Count > 0)
            {
                guaranteedTable.RarityRules.Add(
                    new LootRarityRule
                    {
                        Id = rule.Id,
                        RarityId = rule.RarityId,
                        Weight = rule.Weight,
                        Entries = entries,
                    }
                );
            }
        }

        if (guaranteedTable.RarityRules.Count == 0)
        {
            throw new InvalidLootTableException(
                $"Loot table '{table.Id}' has no equipment item of Rare rarity or higher to guarantee."
            );
        }

        return guaranteedTable;
    }

    private static bool IsGuaranteedLoot(Item item) =>
        IsEquipment(item) && item.Rarity is { Rank: var rank } && rank >= MinimumGuaranteedRank;

    // Equipment is any known category that is not a consumable. A category outside the known
    // ones is managed from the catalogue only and carries no equipment rule.
    private static bool IsEquipment(Item item)
    {
        foreach (ItemCategory category in Enum.GetValues<ItemCategory>())
        {
            if (
                string.Equals(
                    category.ToCode(),
                    item.Category?.Label,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return !category.IsConsumable();
            }
        }

        return false;
    }
}
