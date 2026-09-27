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

public sealed class EquipItemIntegrationTests(InventoryControllerFixture fixture)
    : InventoryControllerTestBase(fixture)
{
    private static readonly Guid WarriorHeroId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task EquipItem_WhenEligible_ReplacesTheExistingEquipment()
    {
        SeededItem oldSword = await SeedItemAsync("SWORD", "RIGHT_HAND", isEquipped: true);
        SeededItem newSword = await SeedItemAsync("SWORD", "RIGHT_HAND");

        HttpResponseMessage response = await Fixture.HttpClient.PutAsJsonAsync(
            $"/api/v1/heroes/{WarriorHeroId}/inventory/items/{newSword.ItemInstanceId}/equipment",
            new EquipItemRequest(newSword.SlotId),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetItemStatusAsync(newSword.ItemInstanceId))
            .Should()
            .Be(ItemInstanceStatus.Equipped);
        (await GetItemStatusAsync(oldSword.ItemInstanceId))
            .Should()
            .Be(ItemInstanceStatus.Available);
        (await GetEquipmentSlotIdsAsync(newSword.ItemInstanceId))
            .Should()
            .BeEquivalentTo([newSword.SlotId]);
        (await GetEquipmentSlotIdsAsync(oldSword.ItemInstanceId)).Should().BeEmpty();
    }

    [Fact]
    public async Task EquipItem_TwoHandedSword_OccupiesBothHandSlots()
    {
        SeededItem sword = await SeedItemAsync(
            "TWO_HANDED_SWORD",
            "RIGHT_HAND",
            createLeftHand: true
        );

        HttpResponseMessage response = await Fixture.HttpClient.PutAsJsonAsync(
            $"/api/v1/heroes/{WarriorHeroId}/inventory/items/{sword.ItemInstanceId}/equipment",
            new EquipItemRequest(sword.SlotId),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetEquipmentSlotNamesAsync(sword.ItemInstanceId))
            .Should()
            .BeEquivalentTo(["RIGHT_HAND", "LEFT_HAND"]);
    }

    [Fact]
    public async Task EquipItem_WhenReserved_ReturnsConflict()
    {
        SeededItem item = await SeedItemAsync(
            "SWORD",
            "RIGHT_HAND",
            status: ItemInstanceStatus.Reserved
        );

        HttpResponseMessage response = await Fixture.HttpClient.PutAsJsonAsync(
            $"/api/v1/heroes/{WarriorHeroId}/inventory/items/{item.ItemInstanceId}/equipment",
            new EquipItemRequest(item.SlotId),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await GetEquipmentSlotIdsAsync(item.ItemInstanceId)).Should().BeEmpty();
    }

    [Fact]
    public async Task EquipItem_WhenListed_ReturnsConflict()
    {
        SeededItem item = await SeedItemAsync("SWORD", "RIGHT_HAND", isListed: true);

        HttpResponseMessage response = await Fixture.HttpClient.PutAsJsonAsync(
            $"/api/v1/heroes/{WarriorHeroId}/inventory/items/{item.ItemInstanceId}/equipment",
            new EquipItemRequest(item.SlotId),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await GetEquipmentSlotIdsAsync(item.ItemInstanceId)).Should().BeEmpty();
    }

    private async Task<SeededItem> SeedItemAsync(
        string categoryName,
        string slotName,
        ItemInstanceStatus status = ItemInstanceStatus.Available,
        bool isEquipped = false,
        bool isListed = false,
        bool createLeftHand = false
    )
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();

        InventoryEntity inventory =
            await dbContext.Inventories.SingleOrDefaultAsync(
                inventory => inventory.HeroId == WarriorHeroId,
                TestContext.Current.CancellationToken
            )
            ?? new InventoryEntity
            {
                Id = Guid.NewGuid(),
                HeroId = WarriorHeroId,
                ItemCapacity = 40,
                PotionCapacity = 20,
                CreatedAt = now,
                UpdatedAt = now,
            };
        Category category =
            await dbContext.Categories.SingleOrDefaultAsync(
                category => category.Label == categoryName,
                TestContext.Current.CancellationToken
            )
            ?? new Category
            {
                Id = Guid.NewGuid(),
                Label = categoryName,
                CreatedAt = now,
                UpdatedAt = now,
            };
        Rarity rarity =
            await dbContext.Rarities.SingleOrDefaultAsync(
                rarity => rarity.Label == "Common",
                TestContext.Current.CancellationToken
            )
            ?? new Rarity
            {
                Id = Guid.NewGuid(),
                Label = "Common",
                Color = "#FFFFFF",
                Rank = 1,
                CreatedAt = now,
                UpdatedAt = now,
            };
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Category = category,
            Rarity = rarity,
            Name = categoryName,
            Description = "Test item.",
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
        EquipmentSlot slot =
            await dbContext.EquipmentSlots.SingleOrDefaultAsync(
                slot => slot.Name == slotName,
                TestContext.Current.CancellationToken
            )
            ?? new EquipmentSlot
            {
                Id = Guid.NewGuid(),
                Name = slotName,
                CreatedAt = now,
                UpdatedAt = now,
            };

        dbContext.Add(itemInstance);
        if (dbContext.Entry(slot).State == EntityState.Detached)
        {
            dbContext.EquipmentSlots.Add(slot);
        }
        if (createLeftHand)
        {
            bool leftHandExists = await dbContext.EquipmentSlots.AnyAsync(
                slot => slot.Name == "LEFT_HAND",
                TestContext.Current.CancellationToken
            );
            if (!leftHandExists)
            {
                dbContext.EquipmentSlots.Add(
                    new EquipmentSlot
                    {
                        Id = Guid.NewGuid(),
                        Name = "LEFT_HAND",
                        CreatedAt = now,
                        UpdatedAt = now,
                    }
                );
            }
        }

        if (isEquipped)
        {
            dbContext.Equipment.Add(
                new Equipment
                {
                    Id = Guid.NewGuid(),
                    HeroId = WarriorHeroId,
                    ItemInstance = itemInstance,
                    Slot = slot,
                    Quantity = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        if (isListed)
        {
            dbContext.MarketplaceListings.Add(
                new MarketplaceListing
                {
                    Id = Guid.NewGuid(),
                    ItemInstance = itemInstance,
                    SellerId = WarriorHeroId,
                    Quantity = 1,
                    Price = 10,
                    Status = "ACTIVE",
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new SeededItem(itemInstance.Id, slot.Id);
    }

    private async Task<ItemInstanceStatus> GetItemStatusAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .ItemInstances.Where(item => item.Id == itemInstanceId)
            .Select(item => item.Status)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> GetEquipmentSlotIdsAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .Equipment.Where(equipment => equipment.ItemInstanceId == itemInstanceId)
            .Select(equipment => equipment.SlotId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetEquipmentSlotNamesAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .Equipment.Where(equipment => equipment.ItemInstanceId == itemInstanceId)
            .Include(equipment => equipment.Slot)
            .Select(equipment => equipment.Slot.Name)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private sealed record EquipItemRequest(Guid SlotId);

    private sealed record SeededItem(Guid ItemInstanceId, Guid SlotId);
}
