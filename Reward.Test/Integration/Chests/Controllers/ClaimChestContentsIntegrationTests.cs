using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Application.Features.RewardUseCase.GenerateChestLoot;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Integration.Chests.Controllers;

public sealed class ClaimChestContentsIntegrationTests(ChestControllerFixture fixture)
    : ChestControllerTestBase(fixture)
{
    [Fact]
    public async Task ClaimContents_WhenChestIsFilled_TransfersEveryRewardAndEmptiesTheChest()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedInventoryAndLootTableAsync(heroId, isPotionCapacityFull: false);
        GenerateChestLootResult loot = await GenerateChestLootAsync(dungeonRunId, chestId);

        // Act
        HttpResponseMessage response = await ClaimAsync(heroId, dungeonRunId, chestId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ClaimChestContentsResponse? body =
            await response.Content.ReadFromJsonAsync<ClaimChestContentsResponse>(
                TestContext.Current.CancellationToken
            );
        body.Should().NotBeNull();
        body!.ChestId.Should().Be(chestId);
        body.State.Should().Be("EMPTY");
        body.AlreadyEmpty.Should().BeFalse();
        body.Items.Select(item => new { item.ItemId, item.Quantity })
            .Should()
            .BeEquivalentTo(loot.Items.Select(item => new { item.ItemId, item.Quantity }));

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        List<ItemInstance> itemInstances = await dbContext
            .ItemInstances.Include(itemInstance => itemInstance.Inventory)
            .ToListAsync(TestContext.Current.CancellationToken);
        itemInstances.Should().NotBeEmpty();
        itemInstances.Should().OnlyContain(itemInstance => itemInstance.Inventory.HeroId == heroId);
        itemInstances
            .Should()
            .OnlyContain(itemInstance => itemInstance.Status == ItemInstanceStatus.Available);
        RewardEntity reward = await dbContext
            .Rewards.Include(value => value.Items)
            .SingleAsync(value => value.Id == loot.RewardId, TestContext.Current.CancellationToken);
        reward.HeroId.Should().Be(heroId);
        reward.Status.Should().Be(RewardStatus.Applied);
        reward.Items.Should().OnlyContain(rewardItem => rewardItem.ItemInstanceId != null);
    }

    [Fact]
    public async Task ClaimContents_WhenChestIsAlreadyEmpty_DoesNotTransferRewardsAgain()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedInventoryAndLootTableAsync(heroId, isPotionCapacityFull: false);
        await GenerateChestLootAsync(dungeonRunId, chestId);
        HttpResponseMessage firstResponse = await ClaimAsync(heroId, dungeonRunId, chestId);
        int itemInstanceCountAfterFirstClaim = await CountItemInstancesAsync();

        // Act
        HttpResponseMessage secondResponse = await ClaimAsync(heroId, dungeonRunId, chestId);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ClaimChestContentsResponse? body =
            await secondResponse.Content.ReadFromJsonAsync<ClaimChestContentsResponse>(
                TestContext.Current.CancellationToken
            );
        body.Should().BeEquivalentTo(new ClaimChestContentsResponse(chestId, "EMPTY", [], true));
        int itemInstanceCount = await CountItemInstancesAsync();
        itemInstanceCount.Should().Be(itemInstanceCountAfterFirstClaim);
    }

    [Fact]
    public async Task ClaimContents_WhenInventoryCapacityIsInsufficient_ReturnsConflictAndKeepsTheChestFilled()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedInventoryAndLootTableAsync(heroId, isPotionCapacityFull: true);
        GenerateChestLootResult loot = await GenerateChestLootAsync(dungeonRunId, chestId);

        // Act
        HttpResponseMessage response = await ClaimAsync(heroId, dungeonRunId, chestId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        int itemInstanceCount = await CountItemInstancesAsync();
        itemInstanceCount.Should().Be(1);
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        RewardEntity reward = await dbContext.Rewards.SingleAsync(
            value => value.Id == loot.RewardId,
            TestContext.Current.CancellationToken
        );
        reward.HeroId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task ClaimContents_WhenChestWasNotGenerated_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await ClaimAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClaimContents_WhenChestIdIsEmpty_ReturnsUnprocessableEntity()
    {
        // Act
        HttpResponseMessage response = await ClaimAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private Task<HttpResponseMessage> ClaimAsync(Guid heroId, Guid dungeonRunId, Guid chestId) =>
        Fixture.HttpClient.PostAsync(
            $"/api/v1/heroes/{heroId}/runs/{dungeonRunId}/chests/{chestId}/claim",
            content: null,
            TestContext.Current.CancellationToken
        );

    // Fills the chest through the real chest loot generation, as the Dungeon service would.
    private async Task<GenerateChestLootResult> GenerateChestLootAsync(
        Guid dungeonRunId,
        Guid chestId
    )
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send<GenerateChestLootResult>(
            new GenerateChestLootCommand(Guid.NewGuid(), dungeonRunId, chestId, 1, "NORMAL"),
            TestContext.Current.CancellationToken
        );
    }

    // Seeds a hero inventory and a floor 1 NORMAL chest loot table that always yields potions.
    // A full inventory already holds as many potion stacks as its potion capacity.
    private async Task SeedInventoryAndLootTableAsync(Guid heroId, bool isPotionCapacityFull)
    {
        const int potionCapacity = 1;
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
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Category = category,
            Rarity = rarity,
            Name = "Health Potion",
            Description = "Restores health.",
            LevelRequired = 1,
            Stackable = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var table = new LootTable
        {
            Id = Guid.NewGuid(),
            Name = "Chest - Floor 1",
            Floor = 1,
            Difficulty = "NORMAL",
            SourceType = "CHEST",
            DrawCount = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var rule = new LootRarityRule
        {
            Id = Guid.NewGuid(),
            LootTable = table,
            Rarity = rarity,
            Weight = 1,
        };
        rule.Entries.Add(
            new LootTableEntry
            {
                Id = Guid.NewGuid(),
                LootRarityRule = rule,
                Item = item,
                Weight = 1,
                MinQuantity = 2,
                MaxQuantity = 2,
            }
        );
        table.RarityRules.Add(rule);
        var source = new RewardSource
        {
            Id = Guid.NewGuid(),
            Name = "CHEST",
            Description = "Dungeon chest loot",
        };
        var inventory = new InventoryEntity
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            ItemCapacity = 40,
            PotionCapacity = potionCapacity,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, item, table, source, inventory);
        if (isPotionCapacityFull)
        {
            dbContext.ItemInstances.Add(
                new ItemInstance
                {
                    Id = Guid.NewGuid(),
                    Item = item,
                    Inventory = inventory,
                    Status = ItemInstanceStatus.Available,
                    Quantity = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<int> CountItemInstancesAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext.ItemInstances.CountAsync(TestContext.Current.CancellationToken);
    }

    private sealed record ClaimChestContentsResponse(
        Guid ChestId,
        string State,
        IReadOnlyList<ClaimedChestItemResponse> Items,
        bool AlreadyEmpty
    );

    private sealed record ClaimedChestItemResponse(Guid ItemInstanceId, Guid ItemId, int Quantity);
}
