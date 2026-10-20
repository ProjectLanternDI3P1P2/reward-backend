using FluentAssertions;
using Moq;
using Reward.Application.Features.RewardUseCase.GenerateChestLoot;
using Reward.Application.Messaging;
using Reward.Contracts.Events.V1;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;
using Reward.Domain.Services;

namespace Reward.Test.Features.RewardUseCase;

public sealed class GenerateChestLootCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewContext_LocksBeforeRandomAndAddsEventAndGeneratedLog()
    {
        // Arrange one exact table and capture externally visible repository effects.
        Guid commandId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        LootTable table = CreateTable();
        var steps = new List<string>();
        var outbox = new List<OutboxMessage>();
        ChestLootGeneration? addedGeneration = null;
        var repository = new Mock<IChestLootRepository>();
        repository
            .Setup(value =>
                value.AcquireGenerationLockAsync(runId, chestId, It.IsAny<CancellationToken>())
            )
            .Callback(() => steps.Add("lock"))
            .Returns(Task.CompletedTask);
        repository
            .Setup(value => value.GetGenerationAsync(runId, chestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChestLootGeneration?)null);
        repository
            .Setup(value => value.GetLootTableAsync(1, "NORMAL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        repository
            .Setup(value => value.GetRewardSourceAsync("CHEST", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RewardSource { Id = Guid.NewGuid(), Name = "CHEST" });
        repository
            .Setup(value => value.AddGeneration(It.IsAny<ChestLootGeneration>()))
            .Callback<ChestLootGeneration>(value => addedGeneration = value);
        repository
            .Setup(value => value.AddOutboxMessage(It.IsAny<OutboxMessage>()))
            .Callback<OutboxMessage>(outbox.Add);
        var handler = new GenerateChestLootCommandHandler(
            repository.Object,
            new LootSelectionService(new CallbackRandom(() => steps.Add("random"))),
            new FixedClock()
        );

        // Act through the application handler.
        GenerateChestLootResult result = await handler.Handle(
            new GenerateChestLootCommand(commandId, runId, chestId, 1, "NORMAL"),
            TestContext.Current.CancellationToken
        );

        // Assert concurrency protection precedes every random choice.
        steps.First().Should().Be("lock");
        steps.Should().Contain("random");

        // Assert generation retains its exact table and aggregated reward graph.
        result.AlreadyGenerated.Should().BeFalse();
        result.LootTableId.Should().Be(table.Id);
        addedGeneration.Should().NotBeNull();
        addedGeneration!.LootTableId.Should().Be(table.Id);
        addedGeneration.Reward.Items.Should().ContainSingle();

        // Assert the first generation emits exactly one event and one structured log.
        outbox
            .Select(message => message.Type)
            .Should()
            .BeEquivalentTo(
                ChestLootOutboxMessages.GeneratedEventType,
                ChestLootOutboxMessages.GenerationLogType
            );
        OutboxMessage eventMessage = outbox.Single(message =>
            message.Type == ChestLootOutboxMessages.GeneratedEventType
        );
        ChestLootGenerated
            .Parser.ParseFrom(eventMessage.Payload)
            .LootTableId.Should()
            .Be(table.Id.ToString());
    }

    [Fact]
    public async Task Handle_ExistingGeneration_ReturnsImmutableReplayWithoutRandomnessOrEvent()
    {
        // Arrange a stored response whose context differs from the retry request.
        Guid runId = Guid.NewGuid();
        Guid chestId = Guid.NewGuid();
        ChestLootGeneration existing = CreateGeneration(runId, chestId);
        var outbox = new List<OutboxMessage>();
        var repository = new Mock<IChestLootRepository>();
        repository
            .Setup(value =>
                value.AcquireGenerationLockAsync(runId, chestId, It.IsAny<CancellationToken>())
            )
            .Returns(Task.CompletedTask);
        repository
            .Setup(value => value.GetGenerationAsync(runId, chestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository
            .Setup(value => value.AddOutboxMessage(It.IsAny<OutboxMessage>()))
            .Callback<OutboxMessage>(outbox.Add);
        var handler = new GenerateChestLootCommandHandler(
            repository.Object,
            new LootSelectionService(new ThrowingRandom()),
            new FixedClock()
        );

        // Act with a different floor and difficulty for the same immutable run/chest key.
        GenerateChestLootResult result = await handler.Handle(
            new GenerateChestLootCommand(Guid.NewGuid(), runId, chestId, 2, "HARD"),
            TestContext.Current.CancellationToken
        );

        // Assert replay returns the original persisted context and never reads a current table.
        result.AlreadyGenerated.Should().BeTrue();
        result.Floor.Should().Be(1);
        result.Difficulty.Should().Be("NORMAL");
        result.LootTableId.Should().Be(existing.LootTableId);
        result.Items.Single().Name.Should().Be("Stored Blade");
        result.Items.Single().Rarity.Should().Be("Rare");
        repository.Verify(
            value =>
                value.GetLootTableAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );

        // Assert replay produces a log but never a duplicate business event.
        outbox.Should().ContainSingle();
        outbox.Single().Type.Should().Be(ChestLootOutboxMessages.GenerationLogType);
        ChestLootGenerationLog
            .Parser.ParseFrom(outbox.Single().Payload)
            .Outcome.Should()
            .Be("REPLAYED");
    }

    private static LootTable CreateTable()
    {
        // Build a deterministic one-item table for handler orchestration tests.
        var rarity = new Rarity { Id = Guid.NewGuid(), Label = "Common" };
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Name = "Health Potion",
            RarityId = rarity.Id,
            Rarity = rarity,
        };
        var table = new LootTable
        {
            Id = Guid.NewGuid(),
            Floor = 1,
            Difficulty = "NORMAL",
            SourceType = "CHEST",
            DrawCount = 1,
        };
        table.RarityRules.Add(
            new LootRarityRule
            {
                RarityId = rarity.Id,
                Weight = 1,
                Entries =
                [
                    new LootTableEntry
                    {
                        ItemId = item.Id,
                        Item = item,
                        Weight = 1,
                        MinQuantity = 1,
                        MaxQuantity = 1,
                    },
                ],
            }
        );
        return table;
    }

    private static ChestLootGeneration CreateGeneration(Guid runId, Guid chestId)
    {
        // Build the complete persisted response graph used by the replay path.
        var rarity = new Rarity { Id = Guid.NewGuid(), Label = "Legendary" };
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Name = "Renamed Blade",
            Rarity = rarity,
        };
        var reward = new Reward.Domain.Entities.Reward
        {
            Id = Guid.NewGuid(),
            Items =
            [
                new RewardItem
                {
                    ItemId = item.Id,
                    Item = item,
                    ItemNameSnapshot = "Stored Blade",
                    ItemRaritySnapshot = "Rare",
                    Quantity = 2,
                },
            ],
        };
        return new ChestLootGeneration
        {
            Id = Guid.NewGuid(),
            DungeonRunId = runId,
            ChestId = chestId,
            LootTableId = Guid.NewGuid(),
            Floor = 1,
            Difficulty = "NORMAL",
            RewardId = reward.Id,
            Reward = reward,
        };
    }

    private sealed class CallbackRandom(Action callback) : IRandomNumberGenerator
    {
        public long NextInt64(long exclusiveMaximum)
        {
            // Record each random request while returning the first valid interval.
            callback();
            return 0;
        }
    }

    private sealed class ThrowingRandom : IRandomNumberGenerator
    {
        public long NextInt64(long exclusiveMaximum) =>
            throw new InvalidOperationException("Replay must not consume randomness.");
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 30, 8, 0, 0, TimeSpan.Zero);
    }
}
