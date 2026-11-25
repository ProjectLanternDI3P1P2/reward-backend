using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.ChestUseCase.ClaimChestContents;
using Reward.Application.Messaging;
using Reward.Domain.Exceptions;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/heroes/{heroId:guid}/runs/{dungeonRunId:guid}/chests")]
public sealed class ChestController(ISender sender, IChestLootLogWriter logWriter) : ControllerBase
{
    [HttpPost("{chestId:guid}/claim")]
    public async Task<IActionResult> ClaimContentsAsync(
        Guid heroId,
        Guid dungeonRunId,
        Guid chestId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ClaimChestContentsResult result = await sender.Send<ClaimChestContentsResult>(
                new ClaimChestContentsCommand(heroId, dungeonRunId, chestId),
                cancellationToken
            );

            return Ok(result);
        }
        catch (InventoryCapacityExceededException)
        {
            // Persist the refusal only after the failed command transaction has unwound; the
            // exception middleware then maps the refusal to its HTTP 409 problem.
            await logWriter.TryWriteCapacityRefusedAsync(
                new RefusedChestLootClaimLog(heroId, dungeonRunId, chestId),
                cancellationToken
            );
            throw;
        }
    }
}
