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
    private const string CapacityErrorCode = "INVENTORY_CAPACITY_INSUFFICIENT";

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
            // The command transaction has rolled back before control reaches the controller.
            await logWriter.TryWriteCapacityRefusedAsync(
                new RefusedChestLootClaimLog(heroId, dungeonRunId, chestId),
                cancellationToken
            );

            var problemDetails = new ProblemDetails
            {
                Type = "https://httpstatuses.com/409",
                Title = "Insufficient inventory capacity",
                Detail =
                    "Free up inventory space to retrieve all chest rewards. No items were transferred.",
                Status = StatusCodes.Status409Conflict,
                Instance = HttpContext.Request.Path,
            };
            problemDetails.Extensions["code"] = CapacityErrorCode;
            return Conflict(problemDetails);
        }
    }
}
