using FluentValidation;
using Grpc.Core;
using MediatR;
using Reward.Application.Features.RewardUseCase.GenerateChestLoot;
using Reward.Application.Messaging;
using Reward.Contracts.V1;
using Reward.Domain.Exceptions;

namespace Reward.Presentation.Grpc.Services;

/// <summary>Exposes idempotent dungeon chest generation to the Dungeon service.</summary>
public sealed class ChestLootGrpcService(ISender sender, IChestLootLogWriter logWriter)
    : RewardLootService.RewardLootServiceBase
{
    /// <summary>Generates or replays loot for one chest and dungeon run.</summary>
    public override async Task<ChestLootReply> GenerateChestLoot(
        GenerateChestLootRequest request,
        ServerCallContext context
    )
    {
        // Normalize caller formatting before validation and exact table lookup.
        string difficulty = request.Difficulty.Trim().ToUpperInvariant();

        // Reject malformed distributed identifiers before entering the command pipeline.
        if (!Guid.TryParse(request.CommandId, out Guid commandId))
        {
            await WriteRejectedAsync(
                request,
                difficulty,
                "command_id must be a GUID.",
                context.CancellationToken
            );
            throw Invalid("command_id must be a GUID.");
        }

        if (!Guid.TryParse(request.DungeonRunId, out Guid dungeonRunId))
        {
            await WriteRejectedAsync(
                request,
                difficulty,
                "dungeon_run_id must be a GUID.",
                context.CancellationToken
            );
            throw Invalid("dungeon_run_id must be a GUID.");
        }

        if (!Guid.TryParse(request.ChestId, out Guid chestId))
        {
            await WriteRejectedAsync(
                request,
                difficulty,
                "chest_id must be a GUID.",
                context.CancellationToken
            );
            throw Invalid("chest_id must be a GUID.");
        }

        GenerateChestLootResult result;
        try
        {
            // Send the normalized command through validation and the database transaction behavior.
            result = await sender.Send<GenerateChestLootResult>(
                new GenerateChestLootCommand(
                    commandId,
                    dungeonRunId,
                    chestId,
                    request.Floor,
                    difficulty
                ),
                context.CancellationToken
            );
        }
        catch (Exception exception)
            when (exception
                    is ValidationException
                        or LootTableNotFoundException
                        or InvalidLootTableException
            )
        {
            // Persist rejection only after the failed command transaction has unwound.
            await WriteRejectedAsync(
                request,
                difficulty,
                exception.Message,
                context.CancellationToken
            );
            throw;
        }

        // Map the application result explicitly to keep transport concerns at the boundary.
        var reply = new ChestLootReply
        {
            RewardId = result.RewardId.ToString(),
            DungeonRunId = result.DungeonRunId.ToString(),
            ChestId = result.ChestId.ToString(),
            Floor = result.Floor,
            Difficulty = result.Difficulty,
            AlreadyGenerated = result.AlreadyGenerated,
        };

        // Preserve the deterministic item order returned by the application layer.
        reply.Items.AddRange(
            result.Items.Select(item => new ChestLootItem
            {
                ItemId = item.ItemId.ToString(),
                Name = item.Name,
                Rarity = item.Rarity,
                Quantity = item.Quantity,
            })
        );
        return reply;
    }

    private async Task WriteRejectedAsync(
        GenerateChestLootRequest request,
        string difficulty,
        string reason,
        CancellationToken cancellationToken
    )
    {
        // Keep the rejected log transport-safe even when identifiers cannot be parsed.
        await logWriter.WriteRejectedAsync(
            new RejectedChestLootLog(
                request.CommandId,
                request.DungeonRunId,
                request.ChestId,
                request.Floor,
                difficulty,
                reason
            ),
            cancellationToken
        );
    }

    private static RpcException Invalid(string detail) =>
        new(new Status(StatusCode.InvalidArgument, detail));
}
