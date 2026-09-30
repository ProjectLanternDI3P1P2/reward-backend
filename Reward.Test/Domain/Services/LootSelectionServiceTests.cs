using FluentAssertions;
using Reward.Domain.Entities;
using Reward.Domain.Exceptions;
using Reward.Domain.Services;

namespace Reward.Test.Domain.Services;

public sealed class LootSelectionServiceTests
{
    [Fact]
    public void Select_RepeatedItem_AggregatesInclusiveQuantities()
    {
        // Arrange a two-draw table whose only item can yield one through three units.
        Item item = new() { Id = Guid.NewGuid() };
        LootTable table = CreateTable(
            2,
            new LootRarityRule
            {
                Weight = 1,
                Entries =
                [
                    new LootTableEntry
                    {
                        ItemId = item.Id,
                        Item = item,
                        Weight = 1,
                        MinQuantity = 1,
                        MaxQuantity = 3,
                    },
                ],
            }
        );
        var service = new LootSelectionService(new SequenceRandom(0, 0, 2, 0, 0, 1));

        // Act across both weighted stages and inclusive quantity draws.
        IReadOnlyList<SelectedLootItem> result = service.Select(table);

        // Assert repeated catalogue items are persisted as one aggregated row.
        result.Should().ContainSingle();
        result.Single().Item.Should().BeSameAs(item);
        result.Single().Quantity.Should().Be(5);
    }

    [Fact]
    public void Select_WeightedRarityAndEntry_UsesBothWeightedStages()
    {
        // Arrange a final rarity interval that is reached only by the requested weighted sample.
        Item common = new() { Id = Guid.NewGuid() };
        Item epic = new() { Id = Guid.NewGuid() };
        LootTable table = CreateTable(
            1,
            new LootRarityRule { Weight = 3, Entries = [FixedEntry(common)] },
            new LootRarityRule { Weight = 1, Entries = [FixedEntry(epic)] }
        );
        var service = new LootSelectionService(new SequenceRandom(3, 0, 0));

        // Act with a sample landing in the epic rarity interval.
        SelectedLootItem result = service.Select(table).Single();

        // Assert selection respected rarity weight before entry weight.
        result.Item.Should().BeSameAs(epic);
    }

    [Fact]
    public void Select_InvalidEntry_ThrowsExplicitConfigurationErrorBeforeRandomness()
    {
        // Arrange a malformed quantity range and a random source that must remain unused.
        Item item = new() { Id = Guid.NewGuid() };
        LootTable table = CreateTable(
            1,
            new LootRarityRule
            {
                Weight = 1,
                Entries =
                [
                    new LootTableEntry
                    {
                        ItemId = item.Id,
                        Item = item,
                        Weight = 1,
                        MinQuantity = 3,
                        MaxQuantity = 2,
                    },
                ],
            }
        );
        var service = new LootSelectionService(new ThrowingRandom());

        // Act against the invalid configuration.
        Action action = () => service.Select(table);

        // Assert callers receive the dedicated explicit configuration error.
        action.Should().Throw<InvalidLootTableException>();
    }

    private static LootTableEntry FixedEntry(Item item) =>
        new()
        {
            ItemId = item.Id,
            Item = item,
            Weight = 1,
            MinQuantity = 1,
            MaxQuantity = 1,
        };

    private static LootTable CreateTable(int draws, params LootRarityRule[] rules)
    {
        // Attach the supplied rules to the minimum table graph needed by the domain service.
        var table = new LootTable { Id = Guid.NewGuid(), DrawCount = draws };
        foreach (LootRarityRule rule in rules)
        {
            table.RarityRules.Add(rule);
        }

        return table;
    }

    private sealed class SequenceRandom(params long[] values) : IRandomNumberGenerator
    {
        private readonly Queue<long> values = new(values);

        public long NextInt64(long exclusiveMaximum)
        {
            // Fail the test if selection requests an unexpected bound or extra sample.
            long value = values.Dequeue();
            value.Should().BeLessThan(exclusiveMaximum);
            return value;
        }
    }

    private sealed class ThrowingRandom : IRandomNumberGenerator
    {
        public long NextInt64(long exclusiveMaximum) =>
            throw new InvalidOperationException("Randomness must not be used.");
    }
}
