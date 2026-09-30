using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Infrastructure.Persistence;
using Reward.Infrastructure.Persistence.Seeding;

namespace Reward.Test.Persistence.Seeding;

public sealed class DataSeederTests
{
    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesBrowsableInventoryDataOnlyOnce()
    {
        // Arrange
        await using RewardDbContext context = CreateInMemoryDbContext();

        // Act
        await DataSeeder.SeedAsync(context, TestContext.Current.CancellationToken);
        int itemInstancesAfterFirstSeed = await context.ItemInstances.CountAsync(
            TestContext.Current.CancellationToken
        );
        int lootTablesAfterFirstSeed = await context.LootTables.CountAsync(
            TestContext.Current.CancellationToken
        );
        await DataSeeder.SeedAsync(context, TestContext.Current.CancellationToken);

        // Assert
        (await context.Inventories.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(3);
        (await context.Categories.CountAsync(TestContext.Current.CancellationToken)).Should().Be(7);
        (await context.Rarities.CountAsync(TestContext.Current.CancellationToken)).Should().Be(5);
        (await context.Equipment.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .BeGreaterThan(0);
        (await context.ItemInstances.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(itemInstancesAfterFirstSeed);

        // Assert the four strict chest contexts and source are independently idempotent.
        (
            await context.RewardSources.CountAsync(
                source => source.Name == "CHEST",
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .Be(1);
        List<LootTable> lootTables = await context
            .LootTables.Include(table => table.RarityRules)
                .ThenInclude(rule => rule.Entries)
            .Where(table => table.SourceType == "CHEST" && table.Floor != null)
            .ToListAsync(TestContext.Current.CancellationToken);
        lootTables.Should().HaveCount(4);
        lootTables
            .Select(table => (table.Floor, table.Difficulty))
            .Should()
            .BeEquivalentTo(
                new (int?, string)[] { (1, "NORMAL"), (1, "HARD"), (2, "NORMAL"), (2, "HARD") }
            );
        lootTables
            .Should()
            .OnlyContain(table => table.RarityRules.All(rule => rule.Entries.Count > 0));
        (await context.LootTables.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(lootTablesAfterFirstSeed);
    }

    private static RewardDbContext CreateInMemoryDbContext() =>
        new(
            new DbContextOptionsBuilder<RewardDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );
}
