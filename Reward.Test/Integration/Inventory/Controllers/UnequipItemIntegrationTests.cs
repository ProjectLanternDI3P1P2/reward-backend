using System.Net;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Application.Features.InventoryUseCase.GetActiveEquipment;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.Inventory.Controllers;

public sealed class UnequipItemIntegrationTests(InventoryControllerFixture fixture)
    : InventoryControllerTestBase(fixture)
{
    private static readonly Guid WarriorHeroId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task UnequipItem_WhenEquipped_FreesTheSlotAndRemovesItsActiveEffects()
    {
        Guid itemInstanceId = await SeedEquippedItemAsync("RIGHT_HAND");

        HttpResponseMessage response = await Fixture.HttpClient.DeleteAsync(
            $"/api/v1/heroes/{WarriorHeroId}/inventory/items/{itemInstanceId}/equipment",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetItemStatusAsync(itemInstanceId)).Should().Be(ItemInstanceStatus.Available);
        (await EquipmentCountAsync(itemInstanceId)).Should().Be(0);
        (await GetActiveEquipmentAsync())
            .Slots.Should()
            .ContainSingle(slot => slot.SlotName == "RIGHT_HAND" && slot.EquippedItem == null);
    }

    [Fact]
    public async Task UnequipItem_WhenTwoHandedItemIsEquipped_FreesBothSlots()
    {
        Guid itemInstanceId = await SeedEquippedItemAsync("RIGHT_HAND", "LEFT_HAND");

        HttpResponseMessage response = await Fixture.HttpClient.DeleteAsync(
            $"/api/v1/heroes/{WarriorHeroId}/inventory/items/{itemInstanceId}/equipment",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EquipmentCountAsync(itemInstanceId)).Should().Be(0);
    }

    [Fact]
    public async Task UnequipItem_WhenRetried_RemainsSuccessfulAndKeepsTheItem()
    {
        Guid itemInstanceId = await SeedEquippedItemAsync("RIGHT_HAND");
        string path = $"/api/v1/heroes/{WarriorHeroId}/inventory/items/{itemInstanceId}/equipment";

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
        (await ItemInstanceExistsAsync(itemInstanceId)).Should().BeTrue();
        (await GetItemStatusAsync(itemInstanceId)).Should().Be(ItemInstanceStatus.Available);
    }

    private async Task<Guid> SeedEquippedItemAsync(params string[] slotNames)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        var inventory = new InventoryEntity
        {
            Id = Guid.NewGuid(),
            HeroId = WarriorHeroId,
            ItemCapacity = 40,
            PotionCapacity = 20,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Label = "SWORD",
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
            Status = ItemInstanceStatus.Equipped,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        List<EquipmentSlot> slots = slotNames
            .Select(name => new EquipmentSlot
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();
        List<Equipment> equipment = slots
            .Select(slot => new Equipment
            {
                Id = Guid.NewGuid(),
                HeroId = WarriorHeroId,
                ItemInstance = itemInstance,
                Slot = slot,
                Quantity = 1,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        dbContext.AddRange(category, rarity, inventory, item, itemInstance);
        dbContext.EquipmentSlots.AddRange(slots);
        dbContext.Equipment.AddRange(equipment);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return itemInstance.Id;
    }

    private async Task<ActiveEquipment> GetActiveEquipmentAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return (
            await sender.Send(
                new GetActiveEquipmentQuery(WarriorHeroId),
                TestContext.Current.CancellationToken
            )
        )!;
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

    private async Task<int> EquipmentCountAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.Equipment.CountAsync(
            equipment => equipment.ItemInstanceId == itemInstanceId,
            TestContext.Current.CancellationToken
        );
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
}
