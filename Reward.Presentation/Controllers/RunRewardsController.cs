using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/runs/{dungeonRunId:guid}/rewards")]
public sealed class RunRewardsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDistributionAsync(
        Guid dungeonRunId,
        CancellationToken cancellationToken
    )
    {
        RunRewardDistribution distribution = await sender.Send(
            new GetRunRewardDistributionQuery(dungeonRunId),
            cancellationToken
        );

        return Ok(distribution);
    }
}
