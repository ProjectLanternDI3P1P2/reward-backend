using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    }

    private static RewardDbContext CreateInMemoryDbContext() =>
        new(
            new DbContextOptionsBuilder<RewardDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );
}
