using Reward.Domain.Entities;
using Reward.Domain.Exceptions;

namespace Reward.Domain.Services;

/// <summary>Represents one aggregated item selected from a loot table.</summary>
public sealed record SelectedLootItem(Item Item, int Quantity);

/// <summary>Selects chest items by rarity weight, entry weight and inclusive quantity range.</summary>
public sealed class LootSelectionService(IRandomNumberGenerator random)
{
    /// <summary>Draws and aggregates the items configured by a loot table.</summary>
    public IReadOnlyList<SelectedLootItem> Select(LootTable table)
    {
        // Reject an unusable table before consuming randomness so failures remain deterministic.
        if (table.DrawCount <= 0 || table.RarityRules.Count == 0)
        {
            throw new InvalidLootTableException(
                $"Loot table '{table.Id}' must define a positive draw count and rarity rules."
            );
        }

        // Validate both weighted stages before the first draw to avoid partial selection.
        if (
            table.RarityRules.Any(rule =>
                rule.Weight <= 0
                || rule.Entries.Count == 0
                || rule.Entries.Any(entry =>
                    entry.Weight <= 0
                    || entry.MinQuantity <= 0
                    || entry.MaxQuantity < entry.MinQuantity
                    || entry.Item is null
                    || entry.Item.RarityId != rule.RarityId
                )
            )
        )
        {
            throw new InvalidLootTableException(
                $"Loot table '{table.Id}' contains an invalid rarity rule or entry."
            );
        }

        // Aggregate repeated draws by catalogue item so the persisted reward is compact and stable.
        var quantities = new Dictionary<Guid, (Item Item, int Quantity)>();
        for (int draw = 0; draw < table.DrawCount; draw++)
        {
            // Pick the rarity first, then an item inside that rarity as required by the table model.
            LootRarityRule rule = PickWeighted(table.RarityRules, value => value.Weight);
            LootTableEntry entry = PickWeighted(rule.Entries, value => value.Weight);

            // Include both quantity bounds while avoiding integer overflow on the exclusive range.
            long range = (long)entry.MaxQuantity - entry.MinQuantity + 1;
            int quantity = checked(entry.MinQuantity + (int)random.NextInt64(range));
            (Item Item, int Quantity) current = quantities.GetValueOrDefault(
                entry.ItemId,
                (entry.Item, 0)
            );
            quantities[entry.ItemId] = (entry.Item, checked(current.Quantity + quantity));
        }

        // Return a deterministic order so first generation and replay serialize identically.
        return quantities
            .Values.OrderBy(value => value.Item.Id)
            .Select(value => new SelectedLootItem(value.Item, value.Quantity))
            .ToList();
    }

    private T PickWeighted<T>(IEnumerable<T> values, Func<T, int> weightSelector)
    {
        // Materialize once because each weighted choice needs a total and a traversal.
        List<T> candidates = values.ToList();
        long total = candidates.Aggregate<T, long>(
            0,
            (sum, value) => checked(sum + weightSelector(value))
        );
        long selected = random.NextInt64(total);

        // Locate the half-open weighted interval containing the sampled value.
        foreach (T candidate in candidates)
        {
            int weight = weightSelector(candidate);
            if (selected < weight)
            {
                return candidate;
            }

            selected -= weight;
        }

        // A valid positive total must always select one interval.
        throw new InvalidLootTableException("Weighted loot selection failed unexpectedly.");
    }
}
