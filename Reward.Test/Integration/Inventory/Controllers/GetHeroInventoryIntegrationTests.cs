using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.Inventory.Controllers;

public sealed class GetHeroInventoryIntegrationTests(InventoryControllerFixture fixture)
    : InventoryControllerTestBase(fixture)
{
    [Fact]
    public async Task GetByHeroId_WhenInventoryExists_ReturnsItemsAndConsumables()
    {
        Guid heroId = Guid.NewGuid();
        Guid swordId = Guid.NewGuid();
        Guid potionId = Guid.NewGuid();
        await SeedInventoryAsync(heroId, swordId, potionId);

        HttpResponseMessage response = await Fixture.HttpClient.GetAsync(
            $"/api/v1/heroes/{heroId}/inventory",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        HeroInventoryResponse? body =
            await response.Content.ReadFromJsonAsync<HeroInventoryResponse>(
                TestContext.Current.CancellationToken
            );
        body.Should()
            .BeEquivalentTo(
                new HeroInventoryResponse(
                    heroId,
                    [
                        new InventoryItemResponse(
                            swordId,
                            "WEAPON",
                            "Obsidian Blade",
                            "Epic",
                            "AVAILABLE",
                            1,
                            true,
                            false
                        ),
                    ],
                    [
                        new InventoryItemResponse(
                            potionId,
                            "POTION",
                            "Health Potion",
                            "Common",
                            "RESERVED",
                            3,
                            false,
                            true
                        ),
                    ]
                )
            );
    }

    [Fact]
    public async Task GetByHeroId_WhenInventoryDoesNotExist_ReturnsNotFound()
    {
        HttpResponseMessage response = await Fixture.HttpClient.GetAsync(
            $"/api/v1/heroes/{Guid.NewGuid()}/inventory",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task SeedInventoryAsync(Guid heroId, Guid swordId, Guid potionId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var weaponCategory = new Category
        {
            Id = Guid.NewGuid(),
            Label = "WEAPON",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potionCategory = new Category
        {
            Id = Guid.NewGuid(),
            Label = "POTION",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var common = new Rarity
        {
            Id = Guid.NewGuid(),
            Label = "Common",
            Color = "#FFFFFF",
            Rank = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var epic = new Rarity
        {
            Id = Guid.NewGuid(),
            Label = "Epic",
            Color = "#A855F7",
            Rank = 4,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var inventory = new InventoryEntity
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = 20,
            PotionCapacity = 10,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var sword = new Item
        {
            Id = Guid.NewGuid(),
            Category = weaponCategory,
            Rarity = epic,
            Name = "Obsidian Blade",
            Description = "A blade.",
            LevelRequired = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potion = new Item
        {
            Id = Guid.NewGuid(),
            Category = potionCategory,
            Rarity = common,
            Name = "Health Potion",
            Description = "A potion.",
            LevelRequired = 1,
            Stackable = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var swordInstance = new ItemInstance
        {
            Id = swordId,
            Item = sword,
            Inventory = inventory,
            Status = ItemInstanceStatus.Available,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potionInstance = new ItemInstance
        {
            Id = potionId,
            Item = potion,
            Inventory = inventory,
            Status = ItemInstanceStatus.Reserved,
            Quantity = 3,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var slot = new EquipmentSlot
        {
            Id = Guid.NewGuid(),
            Name = "MAIN_HAND",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemInstance = swordInstance,
            Slot = slot,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(
            weaponCategory,
            potionCategory,
            common,
            epic,
            inventory,
            sword,
            potion,
            swordInstance,
            potionInstance,
            slot,
            equipment
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed record HeroInventoryResponse(
        Guid HeroId,
        IReadOnlyList<InventoryItemResponse> Items,
        IReadOnlyList<InventoryItemResponse> Consumables
    );

    private sealed record InventoryItemResponse(
        Guid Id,
        string Type,
        string Name,
        string Rarity,
        string State,
        int Quantity,
        bool IsEquipped,
        bool IsReserved
    );
}
