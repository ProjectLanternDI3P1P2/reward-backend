using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.Marketplace.Controllers;

public sealed class GetActiveMarketplaceListingsIntegrationTests(
    MarketplaceControllerFixture fixture
) : MarketplaceControllerTestBase(fixture)
{
    [Fact]
    public async Task GetActiveListings_OnlyActiveListingsExist_ReturnsThemWithSellerAndItemDetails()
    {
        // Arrange
        Guid activeListingId = Guid.NewGuid();
        Guid sellerId = Guid.NewGuid();
        await SeedListingsAsync(activeListingId, sellerId);

        // Act
        HttpResponseMessage response = await Fixture.HttpClient.GetAsync(
            "/api/v1/marketplace/listings",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<ActiveMarketplaceListingResponse>? body = await response.Content.ReadFromJsonAsync<
            List<ActiveMarketplaceListingResponse>
        >(TestContext.Current.CancellationToken);
        body.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new ActiveMarketplaceListingResponse(
                    activeListingId,
                    "Obsidian Blade",
                    "WEAPON",
                    "Epic",
                    1,
                    42.5m,
                    sellerId
                )
            );
    }

    private async Task SeedListingsAsync(Guid activeListingId, Guid sellerId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Label = "WEAPON",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var rarity = new Rarity
        {
            Id = Guid.NewGuid(),
            Label = "Epic",
            Color = "#A855F7",
            Rank = 4,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Category = category,
            Rarity = rarity,
            Name = "Obsidian Blade",
            Description = "A blade.",
            LevelRequired = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var inventory = new InventoryEntity
        {
            Id = Guid.NewGuid(),
            HeroId = sellerId,
            ItemCapacity = 20,
            PotionCapacity = 10,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var activeInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Item = item,
            Inventory = inventory,
            Status = ItemInstanceStatus.Available,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var soldInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Item = item,
            Inventory = inventory,
            Status = ItemInstanceStatus.Sold,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var activeListing = new MarketplaceListing
        {
            Id = activeListingId,
            ItemInstance = activeInstance,
            SellerId = sellerId,
            Quantity = 1,
            Price = 42.5m,
            Status = MarketplaceListingStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var soldListing = new MarketplaceListing
        {
            Id = Guid.NewGuid(),
            ItemInstance = soldInstance,
            SellerId = sellerId,
            Quantity = 1,
            Price = 42.5m,
            Status = MarketplaceListingStatus.Sold,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(
            category,
            rarity,
            item,
            inventory,
            activeInstance,
            soldInstance,
            activeListing,
            soldListing
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed record ActiveMarketplaceListingResponse(
        Guid Id,
        string ItemName,
        string Category,
        string Rarity,
        int Quantity,
        decimal Price,
        Guid SellerId
    );
}
