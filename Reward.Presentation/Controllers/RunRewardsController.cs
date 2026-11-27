using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;
using ILogger = Serilog.ILogger;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/rewards/v1/runs/{dungeonRunId:guid}/rewards")]
public sealed class RunRewardsController(ISender sender, ILogger logger) : ControllerBase
{
    /// <summary>
    /// How the group rewards of a run were distributed, for its participants only.
    /// 401 without a trusted identity, 404 for a non-participant as for an unknown run,
    /// 503 when Player cannot confirm the participation.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDistributionAsync(
        Guid dungeonRunId,
        // Set by the API Gateway from the Keycloak token (ADR-GLOB-004).
        [FromHeader(Name = "X-User-Id")] string? userId,
        CancellationToken cancellationToken
    )
    {
        logger.Information(
            "Received request to get the reward distribution of run {DungeonRunId}.",
            dungeonRunId
        );

        RunRewardDistribution distribution = await sender.Send(
            new GetRunRewardDistributionQuery(dungeonRunId, userId),
            cancellationToken
        );

        return Ok(distribution);
    }
}
