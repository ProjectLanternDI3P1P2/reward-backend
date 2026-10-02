using FluentAssertions;
using Reward.Domain.Entities;
using Reward.Domain.Exceptions;
using Reward.Domain.Services;

namespace Reward.Test.Domain.Services;

public sealed class BossLootSelectionServiceTests
{
    private const int Common = 1;
    private const int Uncommon = 2;
    private const int Rare = 3;
    private const int Epic = 4;

    [Fact]
    public void Select_RegularDrawAlreadyGuaranteed_ReturnsOnlyTheRegularLoot()
    {
        // Arrange a table whose only item already satisfies the guarantee.
        Item sword = CreateItem("SWORD", Rare);
        LootTable table = CreateTable((sword, Weight: 1));

        // Act with exactly one regular draw: any extra draw would exhaust the sequence.
        IReadOnlyList<SelectedLootItem> result = CreateService(0, 0, 0).Select(table);

        // Assert
        result.Should().ContainSingle().Which.Item.Should().BeSameAs(sword);
    }

    [Fact]
    public void Select_RegularDrawWithoutHighRarityEquipment_AddsOneFromTheTable()
    {
        // Arrange a table that mostly drops commons: the regular draw picks the potion.
        Item potion = CreateItem("POTION", Common);
        Item sword = CreateItem("SWORD", Rare);
        LootTable table = CreateTable((potion, Weight: 100), (sword, Weight: 1));

        // Act: one regular draw, then the guaranteed draw.
        IReadOnlyList<SelectedLootItem> result = CreateService(0, 0, 0, 0, 0, 0).Select(table);

        // Assert the potion stays and the guaranteed equipment comes from the same table.
        result.Select(selected => selected.Item.Id).Should().BeEquivalentTo([potion.Id, sword.Id]);
    }

    [Fact]
    public void Select_RegularDrawWithUncommonEquipment_StillAddsARareOne()
    {
        // Arrange an equipment item just below the Rare threshold.
        Item uncommonSword = CreateItem("SWORD", Uncommon);
        Item rareArmor = CreateItem("ARMOR", Rare);
        LootTable table = CreateTable((uncommonSword, Weight: 100), (rareArmor, Weight: 1));

        // Act
        IReadOnlyList<SelectedLootItem> result = CreateService(0, 0, 0, 0, 0, 0).Select(table);

        // Assert
        result
            .Select(selected => selected.Item.Id)
            .Should()
            .BeEquivalentTo([uncommonSword.Id, rareArmor.Id]);
    }

    [Fact]
    public void Select_RegularDrawWithHighRarityConsumable_StillAddsAnEquipment()
    {
        // Arrange a high-rarity item that is not equipment.
        Item epicPotion = CreateItem("POTION", Epic);
        Item rareRing = CreateItem("RING", Rare);
        LootTable table = CreateTable((epicPotion, Weight: 100), (rareRing, Weight: 1));

        // Act
        IReadOnlyList<SelectedLootItem> result = CreateService(0, 0, 0, 0, 0, 0).Select(table);

        // Assert
        result
            .Select(selected => selected.Item.Id)
            .Should()
            .BeEquivalentTo([epicPotion.Id, rareRing.Id]);
    }

    [Fact]
    public void Select_GuaranteedItem_UsesItsQuantityRange()
    {
        // Arrange a guaranteed sword that drops between two and four units.
        Item potion = CreateItem("POTION", Common);
        Item sword = CreateItem("SWORD", Rare);
        LootTable table = CreateTable((potion, Weight: 100), (sword, Weight: 1));
        LootTableEntry swordEntry = table
            .RarityRules.SelectMany(rule => rule.Entries)
            .Single(entry => entry.Item == sword);
        swordEntry.MinQuantity = 2;
        swordEntry.MaxQuantity = 4;

        // Act: the last draw picks the top of the inclusive range.
        IReadOnlyList<SelectedLootItem> result = CreateService(0, 0, 0, 0, 0, 2).Select(table);

        // Assert
        result.Single(selected => selected.Item == sword).Quantity.Should().Be(4);
    }

    [Theory]
    [InlineData("WEAPON")]
    [InlineData("SWORD")]
    [InlineData("TWO_HANDED_SWORD")]
    [InlineData("ARMOR")]
    [InlineData("SHIELD")]
    [InlineData("RING")]
    [InlineData("AMULET")]
    [InlineData("two_handed_sword")]
    public void Select_EveryEquipmentCategory_QualifiesForTheGuarantee(string category)
    {
        // Arrange
        Item item = CreateItem(category, Rare);
        LootTable table = CreateTable((item, Weight: 1));

        // Act
        IReadOnlyList<SelectedLootItem> result = CreateService(0, 0, 0).Select(table);

        // Assert
        result.Should().ContainSingle().Which.Item.Should().BeSameAs(item);
    }

    [Theory]
    [InlineData("POTION")]
    [InlineData("VIAL")]
    [InlineData("FOOD")]
    public void Select_OnlyNonEquipmentCategories_ThrowsBeforeDrawingAnything(string category)
    {
        // Arrange a boss table that can only drop non-equipment items.
        LootTable table = CreateTable((CreateItem(category, Epic), Weight: 1));

        // Act: an empty sequence fails with another exception if any value is drawn.
        Action action = () => CreateService().Select(table);

        // Assert
        action
            .Should()
            .Throw<InvalidLootTableException>()
            .WithMessage("*no equipment item of Rare rarity or higher*");
    }

    [Fact]
    public void Select_OnlyEquipmentBelowRare_ThrowsBeforeDrawingAnything()
    {
        // Arrange
        LootTable table = CreateTable((CreateItem("SWORD", Uncommon), Weight: 1));

        // Act
        Action action = () => CreateService().Select(table);

        // Assert
        action.Should().Throw<InvalidLootTableException>();
    }

    [Fact]
    public void Select_NullTable_Throws()
    {
        // Act
        Action action = () => CreateService().Select(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    private static BossLootSelectionService CreateService(params long[] randomValues) =>
        new(new LootSelectionService(new SequenceRandom(randomValues)));

    private static Item CreateItem(string category, int rank)
    {
        var rarity = new Rarity
        {
            Id = new Guid(rank, 0, 0, new byte[8]),
            Label = $"Rank {rank}",
            Rank = rank,
        };
        return new Item
        {
            Id = Guid.NewGuid(),
            Name = category,
            RarityId = rarity.Id,
            Rarity = rarity,
            Category = new Category { Label = category },
        };
    }

    // Groups the items by rarity, one rule per rarity weighted by its heaviest entry.
    private static LootTable CreateTable(params (Item Item, int Weight)[] entries)
    {
        var table = new LootTable
        {
            Id = Guid.NewGuid(),
            Difficulty = "NORMAL",
            SourceType = "BOSS",
            DrawCount = 1,
        };
        foreach (
            IGrouping<Guid, (Item Item, int Weight)> group in entries.GroupBy(entry =>
                entry.Item.RarityId
            )
        )
        {
            table.RarityRules.Add(
                new LootRarityRule
                {
                    Id = Guid.NewGuid(),
                    RarityId = group.Key,
                    Weight = group.Max(entry => entry.Weight),
                    Entries = group
                        .Select(entry => new LootTableEntry
                        {
                            ItemId = entry.Item.Id,
                            Item = entry.Item,
                            Weight = 1,
                            MinQuantity = 1,
                            MaxQuantity = 1,
                        })
                        .ToList(),
                }
            );
        }

        return table;
    }

    // Replays the given draws in order and fails if more values are requested.
    private sealed class SequenceRandom(params long[] values) : IRandomNumberGenerator
    {
        private int index;

        public long NextInt64(long exclusiveMaximum)
        {
            if (index >= values.Length)
            {
                throw new InvalidOperationException("No more random values are expected.");
            }

            return values[index++];
        }
    }
}
