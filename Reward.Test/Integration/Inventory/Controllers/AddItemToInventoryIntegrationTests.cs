using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.Inventory.Controllers;

public sealed class AddItemToInventoryIntegrationTests(InventoryControllerFixture fixture)
    : InventoryControllerTestBase(fixture)
{
    [Fact]
    public async Task AddItem_WhenSpaceIsAvailable_CreatesAnOwnedItemInstance()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid itemId = await SeedInventoryAndItemAsync(heroId);

        // Act
        HttpResponseMessage response = await Fixture.HttpClient.PostAsJsonAsync(
            $"/api/v1/heroes/{heroId}/inventory/items",
            new AddItemToInventoryRequest(itemId, "reward-1"),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AddItemToInventoryResponse? body =
            await response.Content.ReadFromJsonAsync<AddItemToInventoryResponse>(
                TestContext.Current.CancellationToken
            );
        body.Should().NotBeNull();
        body!.AlreadyExists.Should().BeFalse();

        ItemInstance itemInstance = await GetItemInstanceAsync(body.ItemInstanceId);
        itemInstance.Inventory.HeroId.Should().Be(heroId);
        itemInstance.ItemId.Should().Be(itemId);
        itemInstance.Status.Should().Be(ItemInstanceStatus.Available);
        itemInstance.IdempotencyKey.Should().Be("reward-1");
    }

    [Fact]
    public async Task AddItem_WhenRequestIsRetried_DoesNotDuplicateTheItem()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid itemId = await SeedInventoryAndItemAsync(heroId);
        var request = new AddItemToInventoryRequest(itemId, "reward-1");

        // Act
        HttpResponseMessage firstResponse = await Fixture.HttpClient.PostAsJsonAsync(
            $"/api/v1/heroes/{heroId}/inventory/items",
            request,
            TestContext.Current.CancellationToken
        );
        HttpResponseMessage retryResponse = await Fixture.HttpClient.PostAsJsonAsync(
            $"/api/v1/heroes/{heroId}/inventory/items",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        retryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AddItemToInventoryResponse? firstBody =
            await firstResponse.Content.ReadFromJsonAsync<AddItemToInventoryResponse>(
                TestContext.Current.CancellationToken
            );
        AddItemToInventoryResponse? retryBody =
            await retryResponse.Content.ReadFromJsonAsync<AddItemToInventoryResponse>(
                TestContext.Current.CancellationToken
            );
        firstBody.Should().NotBeNull();
        retryBody.Should().BeEquivalentTo(firstBody with { AlreadyExists = true });
        int itemInstanceCount = await CountItemInstancesAsync();
        itemInstanceCount.Should().Be(1);
    }

    [Fact]
    public async Task AddItem_WhenItemCapacityIsFull_ReturnsConflictWithoutCreatingAnItem()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid itemId = await SeedInventoryAndItemAsync(heroId, itemCapacity: 1);
        await SeedExistingItemInstanceAsync(heroId, itemId);

        // Act
        HttpResponseMessage response = await Fixture.HttpClient.PostAsJsonAsync(
            $"/api/v1/heroes/{heroId}/inventory/items",
            new AddItemToInventoryRequest(itemId, "reward-2"),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        int itemInstanceCount = await CountItemInstancesAsync();
        itemInstanceCount.Should().Be(1);
    }

    private async Task<Guid> SeedInventoryAndItemAsync(Guid heroId, int itemCapacity = 40)
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
            Label = "Common",
            Color = "#FFFFFF",
            Rank = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var inventory = new InventoryEntity
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = itemCapacity,
            PotionCapacity = 20,
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

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, inventory, item);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return item.Id;
    }

    private async Task SeedExistingItemInstanceAsync(Guid heroId, Guid itemId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        InventoryEntity inventory = await dbContext.Inventories.SingleAsync(
            inventory => inventory.HeroId == heroId,
            TestContext.Current.CancellationToken
        );
        dbContext.ItemInstances.Add(
            new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                InventoryId = inventory.Id,
                Status = ItemInstanceStatus.Available,
                Quantity = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            }
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<ItemInstance> GetItemInstanceAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .ItemInstances.Include(itemInstance => itemInstance.Inventory)
            .SingleAsync(
                itemInstance => itemInstance.Id == itemInstanceId,
                TestContext.Current.CancellationToken
            );
    }

    private async Task<int> CountItemInstancesAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.ItemInstances.CountAsync(TestContext.Current.CancellationToken);
    }

    private sealed record AddItemToInventoryRequest(Guid ItemId, string IdempotencyKey);

    private sealed record AddItemToInventoryResponse(Guid ItemInstanceId, bool AlreadyExists);
}
