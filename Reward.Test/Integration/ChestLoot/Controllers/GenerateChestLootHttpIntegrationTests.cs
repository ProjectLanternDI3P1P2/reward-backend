using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Application.Features.RewardUseCase.GenerateChestLoot;
using Reward.Application.Messaging;
using Reward.Contracts.Events.V1;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Infrastructure.Persistence;
using Reward.Presentation.DTO;
using Reward.Test.Integration.ChestLoot.Grpc;
using InventoryEntity = Reward.Domain.Entities.Inventory;

namespace Reward.Test.Integration.ChestLoot.Controllers;

[Collection(ChestLootGrpcCollection.Name)]
public sealed class GenerateChestLootHttpIntegrationTests(ChestLootGrpcFixture fixture)
    : IAsyncLifetime
{
    [Fact]
    public async Task Generate_ExactContext_PersistsRewardWithoutChangingInventory()
    {
        // Arrange two tables to prove the normalized HTTP context selects the exact one.
        SeededContext normal = await SeedContextAsync(1, "NORMAL", includeInventory: true);
        await SeedContextAsync(1, "HARD");
        Guid commandId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        var request = new GenerateChestLootRequest(commandId, runId, 17, 1, " normal ");
        await using AsyncServiceScope beforeScope = fixture.Services.CreateAsyncScope();
        RewardDbContext beforeContext =
            beforeScope.ServiceProvider.GetRequiredService<RewardDbContext>();
        List<InventoryItemState> inventoryBefore = await ReadInventoryAsync(beforeContext);

        // Act through the public HTTP endpoint and the complete MediatR transaction pipeline.
        using HttpClient client = fixture.CreateHttpClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/chest-loot/generate",
            request,
            TestContext.Current.CancellationToken
        );
        GenerateChestLootResult? result =
            await response.Content.ReadFromJsonAsync<GenerateChestLootResult>(
                TestContext.Current.CancellationToken
            );

        // Assert the direct response exposes the exact persisted context and deterministic identity.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.ChestId.Should().Be(ChestLootIdentity.Create(runId, 1, 17));
        result.LootTableId.Should().Be(normal.LootTableId);
        result.Difficulty.Should().Be("NORMAL");
        result.Items.Should().ContainSingle().Which.ItemId.Should().Be(normal.ItemId);

        // Assert one reward/event committed and inventory state remained byte-for-byte equivalent.
        await using AsyncServiceScope afterScope = fixture.Services.CreateAsyncScope();
        RewardDbContext afterContext =
            afterScope.ServiceProvider.GetRequiredService<RewardDbContext>();
        (await afterContext.Rewards.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(1);
        (
            await afterContext.OutboxMessages.CountAsync(
                message => message.Type == ChestLootOutboxMessages.GeneratedEventType,
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .Be(1);
        (await ReadInventoryAsync(afterContext)).Should().BeEquivalentTo(inventoryBefore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Generate_MissingOrNullChestElementId_ReturnsBadRequestWithoutGeneration(
        bool includeNullElementId
    )
    {
        // Arrange an otherwise valid request so only JSON presence is under test.
        await SeedContextAsync(1, "NORMAL", includeInventory: true);
        var body = new Dictionary<string, object?>
        {
            ["commandId"] = Guid.NewGuid(),
            ["dungeonRunId"] = Guid.NewGuid(),
            ["floor"] = 1,
            ["difficulty"] = "NORMAL",
        };
        if (includeNullElementId)
        {
            body["chestElementId"] = null;
        }

        // Act through ASP.NET JSON binding before the controller can derive a chest identity.
        using HttpClient client = fixture.CreateHttpClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/chest-loot/generate",
            body,
            TestContext.Current.CancellationToken
        );

        // Assert omission and null are rejected without silently becoming element zero.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        (await dbContext.Rewards.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.ChestLootGenerations.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(0);
        (
            await dbContext.OutboxMessages.CountAsync(
                message => message.Type == ChestLootOutboxMessages.GeneratedEventType,
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .Be(0);
        (await dbContext.Inventories.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(1);
        ItemInstance inventoryItem = await dbContext.ItemInstances.SingleAsync(
            TestContext.Current.CancellationToken
        );
        inventoryItem.Quantity.Should().Be(7);
        inventoryItem.Status.Should().Be(ItemInstanceStatus.Available);
    }

    [Fact]
    public async Task Generate_ZeroChestElementId_RemainsValid()
    {
        // Arrange the boundary value that represents the first map element.
        await SeedContextAsync(1, "NORMAL");
        Guid runId = Guid.NewGuid();
        var request = new GenerateChestLootRequest(Guid.NewGuid(), runId, 0, 1, "NORMAL");

        // Act through normal JSON binding and generation.
        using HttpClient client = fixture.CreateHttpClient();
        GenerateChestLootResult result = await PostSuccessAsync(client, request);

        // Assert explicit zero is distinct from an absent value and derives its normal identity.
        result.AlreadyGenerated.Should().BeFalse();
        result.ChestId.Should().Be(ChestLootIdentity.Create(runId, 1, 0));
    }

    [Fact]
    public async Task Generate_ReplayAfterTableMutation_ReturnsPersistedSnapshotAndNoSecondEvent()
    {
        // Arrange and generate the chest once from its original live catalogue values.
        SeededContext seeded = await SeedContextAsync(1, "NORMAL");
        Guid runId = Guid.NewGuid();
        var request = new GenerateChestLootRequest(Guid.NewGuid(), runId, 4, 1, "NORMAL");
        using HttpClient client = fixture.CreateHttpClient();
        GenerateChestLootResult first = await PostSuccessAsync(client, request);

        // Mutate table and catalogue values after generation to challenge immutable replay.
        await using (AsyncServiceScope mutationScope = fixture.Services.CreateAsyncScope())
        {
            RewardDbContext context =
                mutationScope.ServiceProvider.GetRequiredService<RewardDbContext>();
            LootTable table = await context.LootTables.SingleAsync(
                value => value.Id == seeded.LootTableId,
                TestContext.Current.CancellationToken
            );
            Item item = await context
                .Items.Include(value => value.Rarity)
                .SingleAsync(
                    value => value.Id == seeded.ItemId,
                    TestContext.Current.CancellationToken
                );
            table.DrawCount = 9;
            item.Name = "Changed after generation";
            item.Rarity.Label = "Changed rarity";
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act as a retry with the original command identifier.
        GenerateChestLootResult replay = await PostSuccessAsync(client, request);

        // Assert the stored response wins over all mutable catalogue values.
        replay.AlreadyGenerated.Should().BeTrue();
        replay.RewardId.Should().Be(first.RewardId);
        replay.Items.Should().BeEquivalentTo(first.Items);
        replay.Items.Single().Name.Should().NotBe("Changed after generation");

        // Assert replay adds only its RabbitMQ log and never a second business event.
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        (await dbContext.Rewards.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (
            await dbContext.OutboxMessages.CountAsync(
                message => message.Type == ChestLootOutboxMessages.GeneratedEventType,
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .Be(1);
        List<byte[]> logPayloads = await dbContext
            .OutboxMessages.Where(message =>
                message.Type == ChestLootOutboxMessages.GenerationLogType
            )
            .Select(message => message.Payload)
            .ToListAsync(TestContext.Current.CancellationToken);
        List<string> outcomes = logPayloads
            .Select(payload => ChestLootGenerationLog.Parser.ParseFrom(payload).Outcome)
            .ToList();
        outcomes.Should().BeEquivalentTo("GENERATED", "REPLAYED");
    }

    [Fact]
    public async Task Generate_ConcurrentCalls_CreateOneRewardAndOneBusinessEvent()
    {
        // Arrange two requests for the same derived run/floor/element identity.
        await SeedContextAsync(1, "NORMAL");
        Guid runId = Guid.NewGuid();
        using HttpClient client = fixture.CreateHttpClient();
        var firstRequest = new GenerateChestLootRequest(Guid.NewGuid(), runId, 3, 1, "NORMAL");
        var secondRequest = firstRequest with { CommandId = Guid.NewGuid() };

        // Act concurrently so PostgreSQL must serialize the existing generation command.
        Task<HttpResponseMessage>[] calls =
        [
            client.PostAsJsonAsync(
                "/api/v1/chest-loot/generate",
                firstRequest,
                TestContext.Current.CancellationToken
            ),
            client.PostAsJsonAsync(
                "/api/v1/chest-loot/generate",
                secondRequest,
                TestContext.Current.CancellationToken
            ),
        ];
        HttpResponseMessage[] responses = await Task.WhenAll(calls);
        GenerateChestLootResult[] results = await Task.WhenAll(
            responses.Select(async response =>
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                return (
                    await response.Content.ReadFromJsonAsync<GenerateChestLootResult>(
                        TestContext.Current.CancellationToken
                    )
                )!;
            })
        );

        // Assert one caller generated while the waiter replayed the same immutable reward.
        results.Count(result => result.AlreadyGenerated).Should().Be(1);
        results.Select(result => result.RewardId).Distinct().Should().ContainSingle();
        results[0].Items.Should().BeEquivalentTo(results[1].Items);

        // Assert the advisory lock protected both persistence and the business event.
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        (await dbContext.ChestLootGenerations.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(1);
        (
            await dbContext.OutboxMessages.CountAsync(
                message => message.Type == ChestLootOutboxMessages.GeneratedEventType,
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Generate_DifferentRunFloorOrElement_CreatesIndependentRewards()
    {
        // Arrange both exact floor tables used by four independent chest contexts.
        await SeedContextAsync(1, "NORMAL");
        await SeedContextAsync(2, "NORMAL");
        Guid firstRun = Guid.NewGuid();
        Guid secondRun = Guid.NewGuid();
        GenerateChestLootRequest[] requests =
        [
            new(Guid.NewGuid(), firstRun, 1, 1, "NORMAL"),
            new(Guid.NewGuid(), firstRun, 2, 1, "NORMAL"),
            new(Guid.NewGuid(), firstRun, 1, 2, "NORMAL"),
            new(Guid.NewGuid(), secondRun, 1, 1, "NORMAL"),
        ];

        // Act sequentially to isolate identity semantics from the concurrency test.
        using HttpClient client = fixture.CreateHttpClient();
        var results = new List<GenerateChestLootResult>();
        foreach (GenerateChestLootRequest request in requests)
        {
            results.Add(await PostSuccessAsync(client, request));
        }

        // Assert every identity component selects a distinct immutable reward.
        results.Select(result => result.ChestId).Should().OnlyHaveUniqueItems();
        results.Select(result => result.RewardId).Should().OnlyHaveUniqueItems();
        results.Should().OnlyContain(result => !result.AlreadyGenerated);
    }

    [Theory]
    [InlineData(-1, 1, "NORMAL", HttpStatusCode.UnprocessableEntity)]
    [InlineData(0, 0, "NORMAL", HttpStatusCode.UnprocessableEntity)]
    [InlineData(0, 1, "ELITE", HttpStatusCode.UnprocessableEntity)]
    [InlineData(0, 4, "NORMAL", HttpStatusCode.NotFound)]
    public async Task Generate_InvalidOrMissingContext_IsAtomicAndWritesRejectedLog(
        int chestElementId,
        int floor,
        string difficulty,
        HttpStatusCode expectedStatus
    )
    {
        // Arrange one valid table and an existing inventory row that failures must not touch.
        await SeedContextAsync(1, "NORMAL", includeInventory: true);
        var request = new GenerateChestLootRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            chestElementId,
            floor,
            difficulty
        );

        // Act through transport, validation, transaction rollback and rejection logging.
        using HttpClient client = fixture.CreateHttpClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/chest-loot/generate",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert the caller receives the explicit error category.
        response.StatusCode.Should().Be(expectedStatus);

        // Assert no reward state or inventory changed while one RabbitMQ rejection log persisted.
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        (await dbContext.Rewards.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.ChestLootGenerations.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(0);
        (await dbContext.ItemInstances.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(1);
        OutboxMessage rejected = await dbContext.OutboxMessages.SingleAsync(
            TestContext.Current.CancellationToken
        );
        rejected.Type.Should().Be(ChestLootOutboxMessages.GenerationLogType);
        ChestLootGenerationLog.Parser.ParseFrom(rejected.Payload).Outcome.Should().Be("REJECTED");
    }

    [Fact]
    public async Task Generate_InvalidTable_RollsBackAllRewardStateAndWritesRejectedLog()
    {
        // Arrange a matching table with no selectable rarity rule.
        await SeedContextAsync(1, "NORMAL", includeRule: false, includeInventory: true);
        var request = new GenerateChestLootRequest(Guid.NewGuid(), Guid.NewGuid(), 0, 1, "NORMAL");

        // Act through selection failure and centralized HTTP exception mapping.
        using HttpClient client = fixture.CreateHttpClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/chest-loot/generate",
            request,
            TestContext.Current.CancellationToken
        );

        // Assert invalid server configuration is a conflict with no partial domain writes.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        (await dbContext.Rewards.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.ChestLootGenerations.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(0);
        (await dbContext.ItemInstances.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(1);
        OutboxMessage rejected = await dbContext.OutboxMessages.SingleAsync(
            TestContext.Current.CancellationToken
        );
        ChestLootGenerationLog.Parser.ParseFrom(rejected.Payload).Outcome.Should().Be("REJECTED");
    }

    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<SeededContext> SeedContextAsync(
        int floor,
        string difficulty,
        bool includeRule = true,
        bool includeInventory = false
    )
    {
        // Build one deterministic item and exact table for this isolated context.
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
            Label = $"Common {floor} {difficulty}",
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
            Name = $"Potion {floor} {difficulty}",
            Description = "Restores health.",
            LevelRequired = 1,
            Stackable = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var table = new LootTable
        {
            Id = Guid.NewGuid(),
            Name = $"Test {floor} {difficulty}",
            Floor = floor,
            Difficulty = difficulty,
            SourceType = "CHEST",
            DrawCount = 2,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Add a fixed one-unit rule unless the test intentionally needs invalid configuration.
        if (includeRule)
        {
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
                    MinQuantity = 1,
                    MaxQuantity = 1,
                }
            );
            table.RarityRules.Add(rule);
        }

        // Add one real inventory item so tests can prove generation never credits inventory.
        InventoryEntity? inventory = null;
        if (includeInventory)
        {
            inventory = new InventoryEntity
            {
                Id = Guid.NewGuid(),
                HeroId = Guid.NewGuid(),
                ItemCapacity = 10,
                PotionCapacity = 10,
                CreatedAt = now,
                UpdatedAt = now,
            };
            inventory.ItemInstances.Add(
                new ItemInstance
                {
                    Id = Guid.NewGuid(),
                    Item = item,
                    Inventory = inventory,
                    Status = ItemInstanceStatus.Available,
                    Quantity = 7,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        // Persist catalogue, optional inventory and the shared CHEST source in setup only.
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, item, table);
        if (inventory is not null)
        {
            dbContext.Add(inventory);
        }
        if (
            !await dbContext.RewardSources.AnyAsync(
                value => value.Name == "CHEST",
                TestContext.Current.CancellationToken
            )
        )
        {
            dbContext.Add(
                new RewardSource
                {
                    Id = Guid.NewGuid(),
                    Name = "CHEST",
                    Description = "Dungeon chest loot",
                }
            );
        }
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new SeededContext(table.Id, item.Id);
    }

    private static async Task<GenerateChestLootResult> PostSuccessAsync(
        HttpClient client,
        GenerateChestLootRequest request
    )
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/chest-loot/generate",
            request,
            TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (
            await response.Content.ReadFromJsonAsync<GenerateChestLootResult>(
                TestContext.Current.CancellationToken
            )
        )!;
    }

    private static Task<List<InventoryItemState>> ReadInventoryAsync(RewardDbContext context) =>
        context
            .ItemInstances.OrderBy(item => item.Id)
            .Select(item => new InventoryItemState(
                item.Id,
                item.ItemId,
                item.InventoryId,
                item.Status,
                item.Quantity
            ))
            .ToListAsync(TestContext.Current.CancellationToken);

    private sealed record SeededContext(Guid LootTableId, Guid ItemId);

    private sealed record InventoryItemState(
        Guid Id,
        Guid ItemId,
        Guid InventoryId,
        ItemInstanceStatus Status,
        int Quantity
    );
}
