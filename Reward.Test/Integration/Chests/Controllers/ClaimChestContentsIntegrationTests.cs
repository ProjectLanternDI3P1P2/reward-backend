using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Application.Features.RewardUseCase.GenerateChestLoot;
using Reward.Application.Messaging;
using Reward.Contracts.Events.V1;
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
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using JsonDocument problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        JsonElement root = problem.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(409);
        root.GetProperty("title").GetString().Should().Be("Insufficient inventory capacity");
        root.GetProperty("code").GetString().Should().Be("INVENTORY_CAPACITY_INSUFFICIENT");
        root.GetProperty("detail")
            .GetString()
            .Should()
            .Be(
                "Free up inventory space to retrieve all chest rewards. No items were transferred."
            );
        int itemInstanceCount = await CountItemInstancesAsync();
        itemInstanceCount.Should().Be(1);
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        RewardEntity reward = await dbContext.Rewards.SingleAsync(
            value => value.Id == loot.RewardId,
            TestContext.Current.CancellationToken
        );
        reward.HeroId.Should().Be(Guid.Empty);
        OutboxMessage refused = await dbContext.OutboxMessages.SingleAsync(
            message => message.Type == ChestLootOutboxMessages.ClaimLogType,
            TestContext.Current.CancellationToken
        );
        ChestLootClaimLog refusedPayload = ChestLootClaimLog.Parser.ParseFrom(refused.Payload);
        refusedPayload.Outcome.Should().Be("CAPACITY_REFUSED");
        refusedPayload.HeroId.Should().Be(heroId.ToString());
        refusedPayload.DungeonRunId.Should().Be(dungeonRunId.ToString());
        refusedPayload.ChestId.Should().Be(chestId.ToString());
        refusedPayload.TransferredItemCount.Should().Be(0);
    }

    [Fact]
    public async Task ClaimContents_WhenOnlyPartOfLootFits_RollsBackEveryChangeAndCanRetry()
    {
        // Arrange a two-unit non-stackable reward with one free item slot.
        Guid heroId = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedInventoryAsync(heroId, itemCapacity: 2, potionCapacity: 1);
        Guid occupyingItemId = await SeedInventoryItemAsync(heroId, "SWORD");
        Guid rewardId = await SeedChestAsync(
            dungeonRunId,
            chestId,
            new LootSpec("SWORD", Stackable: false, Quantity: 2)
        );
        ChestSnapshot before = await ReadChestSnapshotAsync(rewardId);

        // Act: the first unit fits in the tracked graph and the second one refuses capacity.
        HttpResponseMessage refused = await ClaimAsync(heroId, dungeonRunId, chestId);

        // Assert the PostgreSQL transaction kept both inventory and reward snapshots unchanged.
        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadChestSnapshotAsync(rewardId)).Should().BeEquivalentTo(before);

        // Removing the pre-existing item frees both required slots. The immutable chest loot is
        // then claimed in full, with no regeneration or partial residue from the first try.
        await RemoveInventoryItemAsync(occupyingItemId);
        HttpResponseMessage retried = await ClaimAsync(heroId, dungeonRunId, chestId);
        retried.StatusCode.Should().Be(HttpStatusCode.OK);
        ChestSnapshot after = await ReadChestSnapshotAsync(rewardId);
        after.HeroId.Should().Be(heroId);
        after.InventoryItems.Should().HaveCount(2).And.OnlyContain(item => item.Quantity == 1);
        after.RewardItemLinks.Should().OnlyContain(link => link != null);

        IReadOnlyList<ChestLootClaimLog> logs = await ReadClaimLogsAsync();
        logs.Select(log => log.Outcome).Should().BeEquivalentTo("CAPACITY_REFUSED", "CLAIMED");
    }

    [Fact]
    public async Task ClaimContents_UsesSeparateCapacitiesAndStackingRules()
    {
        // Arrange exact capacity for two non-stackable items and one stackable potion stack.
        Guid heroId = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedInventoryAsync(heroId, itemCapacity: 2, potionCapacity: 1);
        Guid rewardId = await SeedChestAsync(
            dungeonRunId,
            chestId,
            new LootSpec("SWORD", Stackable: false, Quantity: 2),
            new LootSpec("POTION", Stackable: true, Quantity: 5)
        );

        // Act
        HttpResponseMessage response = await ClaimAsync(heroId, dungeonRunId, chestId);

        // Assert quantity affects non-stackable slots, while a stack occupies one potion slot.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ChestSnapshot snapshot = await ReadChestSnapshotAsync(rewardId);
        snapshot.InventoryItems.Should().HaveCount(3);
        snapshot.InventoryItems.Count(item => item.Quantity == 1).Should().Be(2);
        snapshot.InventoryItems.Should().ContainSingle(item => item.Quantity == 5);
    }

    [Fact]
    public async Task ClaimContents_ConcurrentSameChest_TransfersOnceAndLogsReplay()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        await SeedInventoryAsync(heroId, itemCapacity: 2, potionCapacity: 1);
        Guid rewardId = await SeedChestAsync(
            dungeonRunId,
            chestId,
            new LootSpec("SWORD", Stackable: false, Quantity: 1)
        );

        // Act two requests through independent HTTP scopes.
        HttpResponseMessage[] responses = await Task.WhenAll(
            ClaimAsync(heroId, dungeonRunId, chestId),
            ClaimAsync(heroId, dungeonRunId, chestId)
        );

        // Assert the advisory chest lock serializes the transfer and the replay.
        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        ChestSnapshot snapshot = await ReadChestSnapshotAsync(rewardId);
        snapshot.InventoryItems.Should().ContainSingle();
        IReadOnlyList<ChestLootClaimLog> logs = await ReadClaimLogsAsync();
        logs.Select(log => log.Outcome).Should().BeEquivalentTo("CLAIMED", "ALREADY_EMPTY");
    }

    [Fact]
    public async Task ClaimContents_ConcurrentDifferentChestsCompetingForLastSlot_ClaimsOnlyOne()
    {
        // Arrange two independent chest locks followed by the same one-slot inventory lock.
        Guid heroId = Guid.NewGuid();
        Guid dungeonRunId = Guid.NewGuid();
        Guid firstChestId = Guid.NewGuid();
        Guid secondChestId = Guid.NewGuid();
        await SeedInventoryAsync(heroId, itemCapacity: 1, potionCapacity: 1);
        Guid firstRewardId = await SeedChestAsync(
            dungeonRunId,
            firstChestId,
            new LootSpec("SWORD", Stackable: false, Quantity: 1)
        );
        Guid secondRewardId = await SeedChestAsync(
            dungeonRunId,
            secondChestId,
            new LootSpec("SWORD", Stackable: false, Quantity: 1)
        );

        // Act
        HttpResponseMessage[] responses = await Task.WhenAll(
            ClaimAsync(heroId, dungeonRunId, firstChestId),
            ClaimAsync(heroId, dungeonRunId, secondChestId)
        );

        // Assert exactly one transaction observes and consumes the final slot.
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        ChestSnapshot first = await ReadChestSnapshotAsync(firstRewardId);
        ChestSnapshot second = await ReadChestSnapshotAsync(secondRewardId);
        new[] { first.HeroId, second.HeroId }.Count(id => id == heroId).Should().Be(1);
        first.InventoryItems.Should().ContainSingle();
        IReadOnlyList<ChestLootClaimLog> logs = await ReadClaimLogsAsync();
        logs.Select(log => log.Outcome).Should().BeEquivalentTo("CLAIMED", "CAPACITY_REFUSED");
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

    private async Task SeedInventoryAsync(Guid heroId, int itemCapacity, int potionCapacity)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.Inventories.Add(
            new InventoryEntity
            {
                Id = Guid.NewGuid(),
                HeroId = heroId,
                ItemCapacity = itemCapacity,
                PotionCapacity = potionCapacity,
                CreatedAt = now,
                UpdatedAt = now,
            }
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Guid> SeedChestAsync(Guid dungeonRunId, Guid chestId, params LootSpec[] loot)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        RewardSource? source = await dbContext.RewardSources.SingleOrDefaultAsync(
            value => value.Name == "CHEST",
            TestContext.Current.CancellationToken
        );
        source ??= new RewardSource
        {
            Id = Guid.NewGuid(),
            Name = "CHEST",
            Description = "Dungeon chest loot",
        };
        LootTable? table = await dbContext.LootTables.SingleOrDefaultAsync(
            value =>
                value.SourceType == "CHEST" && value.Floor == 1 && value.Difficulty == "NORMAL",
            TestContext.Current.CancellationToken
        );
        table ??= new LootTable
        {
            Id = Guid.NewGuid(),
            Name = $"Claim test {chestId}",
            Floor = 1,
            Difficulty = "NORMAL",
            SourceType = "CHEST",
            DrawCount = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var reward = new RewardEntity
        {
            Id = Guid.NewGuid(),
            HeroId = Guid.Empty,
            RunId = dungeonRunId,
            RewardSource = source,
            Type = RewardType.Item,
            Status = RewardStatus.Applied,
            RewardKey = $"chest:{dungeonRunId:N}:{chestId:N}",
            CreatedAt = now,
        };

        foreach (LootSpec spec in loot)
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Label = spec.Category,
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
                Name = $"{spec.Category} {Guid.NewGuid():N}",
                Description = "Claim integration item",
                LevelRequired = 1,
                Stackable = spec.Stackable,
                CreatedAt = now,
                UpdatedAt = now,
            };
            reward.Items.Add(
                new RewardItem
                {
                    Id = Guid.NewGuid(),
                    Reward = reward,
                    Item = item,
                    ItemNameSnapshot = item.Name,
                    ItemRaritySnapshot = rarity.Label,
                    Quantity = spec.Quantity,
                    CreatedAt = now,
                }
            );
        }

        dbContext.ChestLootGenerations.Add(
            new ChestLootGeneration
            {
                Id = Guid.NewGuid(),
                CommandId = Guid.NewGuid(),
                DungeonRunId = dungeonRunId,
                ChestId = chestId,
                LootTable = table,
                Floor = 1,
                Difficulty = "NORMAL",
                Reward = reward,
                CreatedAt = now,
            }
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return reward.Id;
    }

    private async Task<Guid> SeedInventoryItemAsync(Guid heroId, string categoryLabel)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        InventoryEntity inventory = await dbContext.Inventories.SingleAsync(
            value => value.HeroId == heroId,
            TestContext.Current.CancellationToken
        );
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Label = categoryLabel,
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
            Name = $"Existing {categoryLabel}",
            Description = "Occupies capacity before claim",
            LevelRequired = 1,
            Stackable = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var instance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            Item = item,
            Inventory = inventory,
            Status = ItemInstanceStatus.Available,
            Quantity = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.ItemInstances.Add(instance);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return instance.Id;
    }

    private async Task RemoveInventoryItemAsync(Guid itemInstanceId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        ItemInstance item = await dbContext.ItemInstances.SingleAsync(
            value => value.Id == itemInstanceId,
            TestContext.Current.CancellationToken
        );
        dbContext.ItemInstances.Remove(item);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<ChestSnapshot> ReadChestSnapshotAsync(Guid rewardId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        RewardEntity reward = await dbContext
            .Rewards.AsNoTracking()
            .Include(value => value.Items)
            .SingleAsync(value => value.Id == rewardId, TestContext.Current.CancellationToken);
        List<InventoryItemSnapshot> inventoryItems = await dbContext
            .ItemInstances.AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new InventoryItemSnapshot(item.Id, item.ItemId, item.Quantity))
            .ToListAsync(TestContext.Current.CancellationToken);
        return new ChestSnapshot(
            reward.HeroId,
            reward.Items.OrderBy(item => item.Id).Select(item => item.ItemInstanceId).ToList(),
            inventoryItems
        );
    }

    private async Task<IReadOnlyList<ChestLootClaimLog>> ReadClaimLogsAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        List<byte[]> payloads = await dbContext
            .OutboxMessages.AsNoTracking()
            .Where(message => message.Type == ChestLootOutboxMessages.ClaimLogType)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Select(message => message.Payload)
            .ToListAsync(TestContext.Current.CancellationToken);
        return payloads.Select(ChestLootClaimLog.Parser.ParseFrom).ToList();
    }

    private sealed record ClaimChestContentsResponse(
        Guid ChestId,
        string State,
        IReadOnlyList<ClaimedChestItemResponse> Items,
        bool AlreadyEmpty
    );

    private sealed record ClaimedChestItemResponse(Guid ItemInstanceId, Guid ItemId, int Quantity);

    private sealed record LootSpec(string Category, bool Stackable, int Quantity);

    private sealed record InventoryItemSnapshot(Guid Id, Guid ItemId, int Quantity);

    private sealed record ChestSnapshot(
        Guid HeroId,
        IReadOnlyList<Guid?> RewardItemLinks,
        IReadOnlyList<InventoryItemSnapshot> InventoryItems
    );
}
