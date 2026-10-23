using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.RewardUseCase.GenerateChestLoot;
using Reward.Application.Messaging;
using Reward.Domain.Exceptions;
using Reward.Presentation.DTO;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/chest-loot")]
public sealed class ChestLootController(ISender sender, IChestLootLogWriter logWriter)
    : ControllerBase
{
    /// <summary>Generates or replays the immutable loot for a dungeon chest element.</summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateChestLootResult>> GenerateAsync(
        GenerateChestLootRequest request,
        CancellationToken cancellationToken
    )
    {
        // Normalize the public transport value before exact table validation and lookup.
        string difficulty = request.Difficulty?.Trim().ToUpperInvariant() ?? string.Empty;
        Guid chestId = request.ChestId;

        try
        {
            // Reuse the application command, validation and transaction behavior used by gRPC.
            GenerateChestLootResult result = await sender.Send<GenerateChestLootResult>(
                new GenerateChestLootCommand(
                    request.CommandId,
                    request.DungeonRunId,
                    chestId,
                    request.Floor,
                    difficulty
                ),
                cancellationToken
            );
            return Ok(result);
        }
        catch (Exception exception)
            when (exception
                    is ValidationException
                        or LootTableNotFoundException
                        or InvalidLootTableException
            )
        {
            // Persist rejection only after the failed command transaction has unwound.
            await logWriter.WriteRejectedAsync(
                new RejectedChestLootLog(
                    request.CommandId.ToString(),
                    request.DungeonRunId.ToString(),
                    chestId.ToString(),
                    request.Floor,
                    difficulty,
                    exception.Message
                ),
                cancellationToken
            );
            throw;
        }
    }
}
