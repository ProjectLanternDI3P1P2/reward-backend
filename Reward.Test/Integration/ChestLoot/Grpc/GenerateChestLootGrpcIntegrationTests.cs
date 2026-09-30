using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reward.Application.Messaging;
using Reward.Contracts.Events.V1;
using Reward.Contracts.V1;
using Reward.Domain.Entities;
using Reward.Domain.Services;
using Reward.Infrastructure.Messaging;
using Reward.Infrastructure.Persistence;

namespace Reward.Test.Integration.ChestLoot.Grpc;

public sealed class GenerateChestLootGrpcIntegrationTests(ChestLootGrpcFixture fixture)
    : ChestLootGrpcTestBase(fixture)
{
    [Fact]
    public async Task GenerateChestLoot_ExactContext_PersistsRewardItemsAndTransactionalOutbox()
    {
        // Arrange one deterministic strict table and a normalized caller variant.
        SeededContext seeded = await SeedContextAsync(floor: 1, difficulty: "NORMAL");
        Guid runId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        GenerateChestLootRequest request = CreateRequest(
            Guid.NewGuid(),
            runId,
            chestId,
            1,
            " normal "
        );

        // Act through the generated gRPC client and complete transaction pipeline.
        ChestLootReply reply = await Fixture
            .CreateClient()
            .GenerateChestLootAsync(
                request,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ResponseAsync;

        // Assert the transport response exposes normalized context and aggregated loot.
        reply.AlreadyGenerated.Should().BeFalse();
        reply.Difficulty.Should().Be("NORMAL");
        reply.Items.Should().ContainSingle();
        reply.Items.Single().ItemId.Should().Be(seeded.ItemId.ToString());
        reply.Items.Single().Quantity.Should().Be(2);

        // Assert reward, selected table and items committed together.
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        ChestLootGeneration generation = await dbContext
            .ChestLootGenerations.Include(value => value.Reward)
                .ThenInclude(reward => reward.Items)
            .SingleAsync(TestContext.Current.CancellationToken);
        generation.LootTableId.Should().Be(seeded.LootTableId);
        generation.Reward.Items.Should().ContainSingle().Which.Quantity.Should().Be(2);

        // Assert the event and structured GENERATED log are pending in the same database.
        List<OutboxMessage> messages = await dbContext
            .OutboxMessages.OrderBy(message => message.Type)
            .ToListAsync(TestContext.Current.CancellationToken);
        messages.Should().HaveCount(2);
        OutboxMessage generatedEvent = messages.Single(message =>
            message.Type == ChestLootOutboxMessages.GeneratedEventType
        );
        ChestLootGenerated
            .Parser.ParseFrom(generatedEvent.Payload)
            .LootTableId.Should()
            .Be(seeded.LootTableId.ToString());
        ChestLootGenerationLog
            .Parser.ParseFrom(
                messages
                    .Single(message => message.Type == ChestLootOutboxMessages.GenerationLogType)
                    .Payload
            )
            .Outcome.Should()
            .Be("GENERATED");
    }

    [Fact]
    public async Task GenerateChestLoot_Replay_ReturnsStoredContextWithoutSecondEvent()
    {
        // Arrange tables for both contexts so replay behavior cannot be explained by a missing table.
        SeededContext originalContext = await SeedContextAsync(floor: 1, difficulty: "NORMAL");
        await SeedContextAsync(floor: 2, difficulty: "HARD");
        Guid runId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        RewardLootService.RewardLootServiceClient client = Fixture.CreateClient();

        // Act once for generation and once with a different context for the same chest key.
        ChestLootReply first = await client
            .GenerateChestLootAsync(
                CreateRequest(Guid.NewGuid(), runId, chestId, 1, "NORMAL"),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ResponseAsync;

        // Mutate live catalogue labels to prove replay reads reward-item snapshots only.
        await using (AsyncServiceScope mutationScope = Fixture.Services.CreateAsyncScope())
        {
            RewardDbContext mutationContext =
                mutationScope.ServiceProvider.GetRequiredService<RewardDbContext>();
            Item item = await mutationContext
                .Items.Include(value => value.Rarity)
                .SingleAsync(
                    value => value.Id == originalContext.ItemId,
                    TestContext.Current.CancellationToken
                );
            item.Name = "Renamed after generation";
            item.Rarity.Label = "Changed after generation";
            await mutationContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Replay after catalogue mutation and with a different requested context.
        ChestLootReply replay = await client
            .GenerateChestLootAsync(
                CreateRequest(Guid.NewGuid(), runId, chestId, 2, "HARD"),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ResponseAsync;

        // Assert immutable persisted values win over the retry's changed context.
        replay.AlreadyGenerated.Should().BeTrue();
        replay.RewardId.Should().Be(first.RewardId);
        replay.Floor.Should().Be(1);
        replay.Difficulty.Should().Be("NORMAL");
        replay.Items.Should().BeEquivalentTo(first.Items);

        // Assert replay adds only its structured log, never another reward or business event.
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
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
    public async Task GenerateChestLoot_DifferentDungeonRunsWithSameChestRemainIndependent()
    {
        // Arrange one table and reuse a chest identifier across two distinct dungeon runs.
        await SeedContextAsync(floor: 1, difficulty: "NORMAL");
        Guid chestId = Guid.NewGuid();
        Guid firstRunId = Guid.NewGuid();
        Guid secondRunId = Guid.NewGuid();
        RewardLootService.RewardLootServiceClient client = Fixture.CreateClient();

        // Act once for each run so each business idempotency key is distinct.
        ChestLootReply firstRun = await client
            .GenerateChestLootAsync(
                CreateRequest(Guid.NewGuid(), firstRunId, chestId, 1, "NORMAL"),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ResponseAsync;
        ChestLootReply secondRun = await client
            .GenerateChestLootAsync(
                CreateRequest(Guid.NewGuid(), secondRunId, chestId, 1, "NORMAL"),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ResponseAsync;

        // Assert the shared chest ID does not collapse rewards from separate runs.
        firstRun.AlreadyGenerated.Should().BeFalse();
        secondRun.AlreadyGenerated.Should().BeFalse();
        firstRun.DungeonRunId.Should().Be(firstRunId.ToString());
        secondRun.DungeonRunId.Should().Be(secondRunId.ToString());
        firstRun.RewardId.Should().NotBe(secondRun.RewardId);

        // Assert both generations and both first-generation events were committed.
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        (await dbContext.ChestLootGenerations.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(2);
        (
            await dbContext.OutboxMessages.CountAsync(
                message => message.Type == ChestLootOutboxMessages.GeneratedEventType,
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .Be(2);
    }

    [Fact]
    public async Task GenerateChestLoot_MissingExactTable_ReturnsNotFoundAndRejectedLog()
    {
        // Arrange only a legacy floorless table which strict lookup must ignore.
        await SeedContextAsync(floor: null, difficulty: "NORMAL");
        GenerateChestLootRequest request = CreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "NORMAL"
        );

        // Act against a context with no exact non-null floor.
        Func<Task> action = async () =>
            await Fixture
                .CreateClient()
                .GenerateChestLootAsync(
                    request,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ResponseAsync;

        // Assert the explicit missing-table status reaches the caller.
        RpcException exception = (await action.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.NotFound);

        // Assert rejection survives the rolled-back command transaction through its independent outbox write.
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        OutboxMessage rejected = await dbContext.OutboxMessages.SingleAsync(
            TestContext.Current.CancellationToken
        );
        ChestLootGenerationLog.Parser.ParseFrom(rejected.Payload).Outcome.Should().Be("REJECTED");
    }

    [Fact]
    public async Task GenerateChestLoot_InvalidTable_ReturnsFailedPreconditionAndRejectedLog()
    {
        // Arrange an exact table with no rarity rules so selection rejects its configuration.
        await SeedContextAsync(floor: 1, difficulty: "NORMAL", includeRule: false);
        GenerateChestLootRequest request = CreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "NORMAL"
        );

        // Act against the invalid server configuration.
        Func<Task> action = async () =>
            await Fixture
                .CreateClient()
                .GenerateChestLootAsync(
                    request,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ResponseAsync;

        // Assert invalid configuration is distinct from caller validation and missing data.
        RpcException exception = (await action.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.FailedPrecondition);

        // Assert the failure still produces the required structured Rabbit outbox log.
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        OutboxMessage rejected = await dbContext.OutboxMessages.SingleAsync(
            TestContext.Current.CancellationToken
        );
        ChestLootGenerationLog.Parser.ParseFrom(rejected.Payload).Outcome.Should().Be("REJECTED");
    }

    [Fact]
    public async Task GenerateChestLoot_InvalidDifficulty_ReturnsInvalidArgumentAndRejectedLog()
    {
        // Arrange a request whose normalized difficulty still contains an unsupported character.
        GenerateChestLootRequest request = CreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "not-valid!"
        );

        // Act through FluentValidation and the centralized gRPC exception mapper.
        Func<Task> action = async () =>
            await Fixture
                .CreateClient()
                .GenerateChestLootAsync(
                    request,
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ResponseAsync;

        // Assert malformed caller context remains distinct from server configuration errors.
        RpcException exception = (await action.Should().ThrowAsync<RpcException>()).Which;
        exception.StatusCode.Should().Be(StatusCode.InvalidArgument);

        // Assert validation rejection is also delivered through the structured log outbox.
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        OutboxMessage rejected = await dbContext.OutboxMessages.SingleAsync(
            TestContext.Current.CancellationToken
        );
        ChestLootGenerationLog.Parser.ParseFrom(rejected.Payload).Outcome.Should().Be("REJECTED");
    }

    [Fact]
    public async Task GenerateChestLoot_ConcurrentCalls_CreateOneRewardAndOneBusinessEvent()
    {
        // Arrange two independent commands targeting the same run/chest lock key.
        await SeedContextAsync(floor: 1, difficulty: "NORMAL");
        Guid runId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        RewardLootService.RewardLootServiceClient client = Fixture.CreateClient();

        // Act concurrently so PostgreSQL must serialize both command transactions.
        Task<ChestLootReply>[] calls =
        [
            client
                .GenerateChestLootAsync(
                    CreateRequest(Guid.NewGuid(), runId, chestId, 1, "NORMAL"),
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ResponseAsync,
            client
                .GenerateChestLootAsync(
                    CreateRequest(Guid.NewGuid(), runId, chestId, 1, "NORMAL"),
                    cancellationToken: TestContext.Current.CancellationToken
                )
                .ResponseAsync,
        ];
        ChestLootReply[] replies = await Task.WhenAll(calls);

        // Assert one call generated and the waiter replayed exactly the same reward.
        replies.Count(reply => reply.AlreadyGenerated).Should().Be(1);
        replies.Select(reply => reply.RewardId).Distinct().Should().ContainSingle();
        replies[0].Items.Should().BeEquivalentTo(replies[1].Items);

        // Assert database uniqueness and event-on-first-only behavior under real concurrency.
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        (await dbContext.ChestLootGenerations.CountAsync(TestContext.Current.CancellationToken))
            .Should()
            .Be(1);
        (await dbContext.Rewards.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
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
    public async Task DispatchNextAsync_PendingMessages_PublishesAndMarksThem()
    {
        // Arrange two pending rows by completing one successful generation.
        await SeedContextAsync(floor: 1, difficulty: "NORMAL");
        await Fixture
            .CreateClient()
            .GenerateChestLootAsync(
                CreateRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "NORMAL"),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ResponseAsync;
        var publisher = new Mock<IMessagePublisher>();
        publisher
            .Setup(value =>
                value.PublishAsync(It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>())
            )
            .Returns(Task.CompletedTask);
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        var dispatcher = new OutboxDispatcher(dbContext, publisher.Object, new FixedClock());

        // Act until the PostgreSQL pending scan reports an empty queue.
        int dispatched = 0;
        while (await dispatcher.DispatchNextAsync(TestContext.Current.CancellationToken))
        {
            dispatched++;
        }

        // Assert both event and log reached the publisher and were durably acknowledged.
        dispatched.Should().Be(2);
        publisher.Verify(
            value => value.PublishAsync(It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
        List<OutboxMessage> messages = await dbContext.OutboxMessages.ToListAsync(
            TestContext.Current.CancellationToken
        );
        messages
            .Should()
            .OnlyContain(message => message.PublishedAtUtc != null && message.Attempts == 1);
    }

    [Fact]
    public async Task DispatchNextAsync_PublisherFailure_LeavesMessagePendingForRetry()
    {
        // Arrange one pending row and a publisher failure representing nack or mandatory return.
        await SeedContextAsync(floor: 1, difficulty: "NORMAL");
        await Fixture
            .CreateClient()
            .GenerateChestLootAsync(
                CreateRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "NORMAL"),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ResponseAsync;
        var publisher = new Mock<IMessagePublisher>();
        publisher
            .Setup(value =>
                value.PublishAsync(It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Broker rejected publication."));
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        var dispatcher = new OutboxDispatcher(dbContext, publisher.Object, new FixedClock());

        // Act through the dispatcher failure path that records retry metadata.
        Func<Task> action = async () =>
            await dispatcher.DispatchNextAsync(TestContext.Current.CancellationToken);

        // Assert the broker exception remains visible to the worker.
        await action.Should().ThrowAsync<InvalidOperationException>();

        // Assert no publication timestamp is written before a successful publisher confirmation.
        dbContext.ChangeTracker.Clear();
        OutboxMessage pending = await dbContext
            .OutboxMessages.OrderBy(message => message.Attempts)
            .ThenBy(message => message.OccurredAtUtc)
            .LastAsync(TestContext.Current.CancellationToken);
        pending.PublishedAtUtc.Should().BeNull();
        pending.Attempts.Should().Be(1);
        pending.LastError.Should().Contain("Broker rejected publication");
    }

    private async Task<SeededContext> SeedContextAsync(
        int? floor,
        string difficulty,
        bool includeRule = true
    )
    {
        // Build explicit catalogue and source rows shared by this context only.
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
        var source = new RewardSource
        {
            Id = Guid.NewGuid(),
            Name = "CHEST",
            Description = "Dungeon chest loot",
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

        // Add a fixed one-unit entry unless the test intentionally needs an invalid table.
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

        // Persist the complete test graph in one setup transaction.
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(category, rarity, item, table);
        if (
            !await dbContext.RewardSources.AnyAsync(
                value => value.Name == "CHEST",
                TestContext.Current.CancellationToken
            )
        )
        {
            dbContext.Add(source);
        }
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new SeededContext(table.Id, item.Id);
    }

    private static GenerateChestLootRequest CreateRequest(
        Guid commandId,
        Guid runId,
        Guid chestId,
        int floor,
        string difficulty
    ) =>
        new()
        {
            CommandId = commandId.ToString(),
            DungeonRunId = runId.ToString(),
            ChestId = chestId.ToString(),
            Floor = floor,
            Difficulty = difficulty,
        };

    private sealed record SeededContext(Guid LootTableId, Guid ItemId);

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 30, 9, 0, 0, TimeSpan.Zero);
    }
}
