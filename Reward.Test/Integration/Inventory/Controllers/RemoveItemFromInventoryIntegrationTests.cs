using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.Inventory.Controllers;

public sealed class RemoveItemFromInventoryIntegrationTests(InventoryControllerFixture fixture)
    : InventoryControllerTestBase(fixture)
{
    [Fact]
    public async Task RemoveItem_WhenOwnedAndAvailable_DeletesTheItem()
    {
        Guid heroId = Guid.NewGuid();
        Guid itemInstanceId = await SeedItemInstanceAsync(heroId, ItemInstanceStatus.Available);

        HttpResponseMessage response = await Fixture.HttpClient.DeleteAsync(
            $"/api/v1/heroes/{heroId}/inventory/items/{itemInstanceId}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ItemInstanceExistsAsync(itemInstanceId)).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveItem_WhenReserved_ReturnsConflictAndKeepsTheItem()
    {
        Guid heroId = Guid.NewGuid();
        Guid itemInstanceId = await SeedItemInstanceAsync(heroId, ItemInstanceStatus.Reserved);

        HttpResponseMessage response = await Fixture.HttpClient.DeleteAsync(
            $"/api/v1/heroes/{heroId}/inventory/items/{itemInstanceId}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ItemInstanceExistsAsync(itemInstanceId)).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveItem_WhenEquipped_UnequipsThenDeletesTheItem()
    {
        Guid heroId = Guid.NewGuid();
        Guid itemInstanceId = await SeedItemInstanceAsync(
            heroId,
            ItemInstanceStatus.Equipped,
            isEquipped: true
        );

        HttpResponseMessage response = await Fixture.HttpClient.DeleteAsync(
            $"/api/v1/heroes/{heroId}/inventory/items/{itemInstanceId}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ItemInstanceExistsAsync(itemInstanceId)).Should().BeFalse();
        (await EquipmentExistsForItemAsync(itemInstanceId)).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveItem_WhenRetried_RemainsSuccessfulWithoutRestoringTheItem()
    {
        Guid heroId = Guid.NewGuid();
        Guid itemInstanceId = await SeedItemInstanceAsync(heroId, ItemInstanceStatus.Available);
        string path = $"/api/v1/heroes/{heroId}/inventory/items/{itemInstanceId}";

        HttpResponseMessage firstResponse = await Fixture.HttpClient.DeleteAsync(
            path,
            TestContext.Current.CancellationToken
        );
        HttpResponseMessage retryResponse = await Fixture.HttpClient.DeleteAsync(
            path,
            TestContext.Current.CancellationToken
        );

        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        retryResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ItemInstanceExistsAsync(itemInstanceId)).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveItem_WhenItBelongsToAnotherHero_LeavesItUntouched()
    {
        Guid ownerHeroId = Guid.NewGuid();
        Guid itemInstanceId = await SeedItemInstanceAsync(
            ownerHeroId,
            ItemInstanceStatus.Available
        );

        HttpResponseMessage response = await Fixture.HttpClient.DeleteAsync(
            $"/api/v1/heroes/{Guid.NewGuid()}/inventory/items/{itemInstanceId}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ItemInstanceExistsAsync(itemInstanceId)).Should().BeTrue();
    }

    private async Task<Guid> SeedItemInstanceAsync(
        Guid heroId,
        ItemInstanceStatus status,
        bool isEquipped = false
    )
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
            Name = "Obsidian Blade",
            Description = "A blade.",
            LevelRequired = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Item = item,
            Inventory = inventory,
            Status = status,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, inventory, item, itemInstance);
        if (isEquipped)
        {
            var slot = new EquipmentSlot
            {
                Id = Guid.NewGuid(),
                Name = "MAIN_HAND",
                CreatedAt = now,
                UpdatedAt = now,
            };
            dbContext.AddRange(
                slot,
                new Equipment
                {
                    Id = Guid.NewGuid(),
                    HeroId = heroId,
                    ItemInstance = itemInstance,
                    Slot = slot,
                    Quantity = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return itemInstance.Id;
    }

    private async Task<bool> ItemInstanceExistsAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.ItemInstances.AnyAsync(
            itemInstance => itemInstance.Id == itemInstanceId,
            TestContext.Current.CancellationToken
        );
    }

    private async Task<bool> EquipmentExistsForItemAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.Equipment.AnyAsync(
            equipment => equipment.ItemInstanceId == itemInstanceId,
            TestContext.Current.CancellationToken
        );
    }
}
