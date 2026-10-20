using Google.Protobuf;
using Reward.Application.Features.RewardUseCase.GenerateChestLoot;
using Reward.Contracts.Events.V1;
using Reward.Domain.Entities;

namespace Reward.Application.Messaging;

/// <summary>Builds the versioned event and structured logs produced by chest generation.</summary>
public static class ChestLootOutboxMessages
{
    public const string GeneratedEventType = "rewards.chest-loot-generated";
    public const string GenerationLogType = "logs.reward.chest-loot-generation";

    /// <summary>Creates the first-generation business event.</summary>
    public static OutboxMessage CreateGeneratedEvent(
        Guid commandId,
        GenerateChestLootResult result,
        DateTimeOffset occurredAt
    )
    {
        // Copy the immutable result into the public versioned payload.
        var payload = new ChestLootGenerated
        {
            CommandId = commandId.ToString(),
            RewardId = result.RewardId.ToString(),
            DungeonRunId = result.DungeonRunId.ToString(),
            ChestId = result.ChestId.ToString(),
            LootTableId = result.LootTableId.ToString(),
            Floor = result.Floor,
            Difficulty = result.Difficulty,
        };

        // Preserve the deterministic response order in the integration event.
        payload.Items.AddRange(
            result.Items.Select(item => new ChestLootGeneratedItem
            {
                ItemId = item.ItemId.ToString(),
                Name = item.Name,
                Rarity = item.Rarity,
                Quantity = item.Quantity,
            })
        );

        // Use the command as correlation and causation for the originating request.
        return Create(commandId.ToString(), GeneratedEventType, occurredAt, payload.ToByteArray());
    }

    /// <summary>Creates a structured GENERATED or REPLAYED business log.</summary>
    public static OutboxMessage CreateGenerationLog(
        Guid commandId,
        GenerateChestLootResult result,
        string outcome,
        DateTimeOffset occurredAt
    )
    {
        // Record identifiers and outcome as fields rather than an interpolated log line.
        var payload = new ChestLootGenerationLog
        {
            CommandId = commandId.ToString(),
            RewardId = result.RewardId.ToString(),
            DungeonRunId = result.DungeonRunId.ToString(),
            ChestId = result.ChestId.ToString(),
            LootTableId = result.LootTableId.ToString(),
            Floor = result.Floor,
            Difficulty = result.Difficulty,
            Outcome = outcome,
            AlreadyGenerated = result.AlreadyGenerated,
            ItemCount = result.Items.Count,
        };

        // Enqueue the log beside the state change that produced it.
        return Create(commandId.ToString(), GenerationLogType, occurredAt, payload.ToByteArray());
    }

    /// <summary>Creates a structured REJECTED business log from the original transport values.</summary>
    public static OutboxMessage CreateRejectedLog(
        RejectedChestLootLog log,
        DateTimeOffset occurredAt
    )
    {
        // Keep raw identifiers because malformed requests may not contain GUID values.
        var payload = new ChestLootGenerationLog
        {
            CommandId = log.CommandId,
            DungeonRunId = log.DungeonRunId,
            ChestId = log.ChestId,
            Floor = log.Floor,
            Difficulty = log.Difficulty,
            Outcome = "REJECTED",
            Reason = log.Reason,
        };

        // Fall back to a generated correlation ID when command_id itself is malformed.
        string correlationId = Guid.TryParse(log.CommandId, out Guid commandId)
            ? commandId.ToString()
            : Guid.NewGuid().ToString();
        return Create(correlationId, GenerationLogType, occurredAt, payload.ToByteArray());
    }

    private static OutboxMessage Create(
        string correlationId,
        string type,
        DateTimeOffset occurredAt,
        byte[] payload
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            CausationId = correlationId,
            Type = type,
            Version = 1,
            OccurredAtUtc = occurredAt,
            Producer = "reward",
            Payload = payload,
        };
}
