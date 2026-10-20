using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.ChestUseCase.ClaimChestContents;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/heroes/{heroId:guid}/runs/{dungeonRunId:guid}/chests")]
public sealed class ChestController(ISender sender) : ControllerBase
{
    [HttpPost("{chestId:guid}/claim")]
    public async Task<IActionResult> ClaimContentsAsync(
        Guid heroId,
        Guid dungeonRunId,
        Guid chestId,
        CancellationToken cancellationToken
    )
    {
        ClaimChestContentsResult result = await sender.Send<ClaimChestContentsResult>(
            new ClaimChestContentsCommand(heroId, dungeonRunId, chestId),
            cancellationToken
        );

        return Ok(result);
    }
}
