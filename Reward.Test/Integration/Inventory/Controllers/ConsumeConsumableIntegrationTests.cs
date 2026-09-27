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

public sealed class ConsumeConsumableIntegrationTests(InventoryControllerFixture fixture)
    : InventoryControllerTestBase(fixture)
{
    [Fact]
    public async Task Consume_WhenAvailable_DecreasesQuantityAndMakesRetriesIdempotent()
    {
        Guid heroId = Guid.NewGuid();
        Guid itemInstanceId = await SeedConsumableAsync(heroId, quantity: 2);
        var request = new ConsumeRequest("consume-1");
        string path = $"/api/v1/heroes/{heroId}/inventory/items/{itemInstanceId}/consume";

        HttpResponseMessage firstResponse = await Fixture.HttpClient.PostAsJsonAsync(
            path,
            request,
            TestContext.Current.CancellationToken
        );
        HttpResponseMessage retryResponse = await Fixture.HttpClient.PostAsJsonAsync(
            path,
            request,
            TestContext.Current.CancellationToken
        );

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        retryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            await firstResponse.Content.ReadFromJsonAsync<ConsumeResponse>(
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .Be(new ConsumeResponse(itemInstanceId, 1, false));
        (
            await retryResponse.Content.ReadFromJsonAsync<ConsumeResponse>(
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .Be(new ConsumeResponse(itemInstanceId, 1, true));
        (await GetQuantityAsync(itemInstanceId)).Should().Be(1);
    }

    [Fact]
    public async Task Consume_WhenReserved_ReturnsConflictWithoutChangingQuantity()
    {
        Guid heroId = Guid.NewGuid();
        Guid itemInstanceId = await SeedConsumableAsync(heroId, 2, ItemInstanceStatus.Reserved);

        HttpResponseMessage response = await Fixture.HttpClient.PostAsJsonAsync(
            $"/api/v1/heroes/{heroId}/inventory/items/{itemInstanceId}/consume",
            new ConsumeRequest("consume-1"),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await GetQuantityAsync(itemInstanceId)).Should().Be(2);
    }

    [Fact]
    public async Task Consume_WhenLastQuantity_IsRemovedButTheRetryIsRemembered()
    {
        Guid heroId = Guid.NewGuid();
        Guid itemInstanceId = await SeedConsumableAsync(heroId, quantity: 1);

        HttpResponseMessage response = await Fixture.HttpClient.PostAsJsonAsync(
            $"/api/v1/heroes/{heroId}/inventory/items/{itemInstanceId}/consume",
            new ConsumeRequest("consume-1"),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ItemInstanceExistsAsync(itemInstanceId)).Should().BeFalse();
        (await ConsumableUseCountAsync()).Should().Be(1);
    }

    private async Task<Guid> SeedConsumableAsync(
        Guid heroId,
        int quantity,
        ItemInstanceStatus status = ItemInstanceStatus.Available
    )
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Label = "POTION",
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
            ItemCapacity = 40,
            PotionCapacity = 20,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Category = category,
            Rarity = rarity,
            Name = "Health Potion",
            Description = "A potion.",
            LevelRequired = 1,
            Stackable = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Item = item,
            Inventory = inventory,
            Status = status,
            Quantity = quantity,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, inventory, item, itemInstance);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return itemInstance.Id;
    }

    private async Task<int> GetQuantityAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .ItemInstances.Where(item => item.Id == itemInstanceId)
            .Select(item => item.Quantity)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<bool> ItemInstanceExistsAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.ItemInstances.AnyAsync(
            item => item.Id == itemInstanceId,
            TestContext.Current.CancellationToken
        );
    }

    private async Task<int> ConsumableUseCountAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.ConsumableUses.CountAsync(TestContext.Current.CancellationToken);
    }

    private sealed record ConsumeRequest(string IdempotencyKey);

    private sealed record ConsumeResponse(
        Guid ItemInstanceId,
        int RemainingQuantity,
        bool AlreadyConsumed
    );
}
