using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/rewards/v1/runs/{dungeonRunId:guid}/rewards")]
public sealed class RunRewardsController(ISender sender, IHttpClientFactory clients)
    : ControllerBase
{
    public const string PlayerMembershipClient = "PlayerMembership";

    [HttpGet]
    public async Task<IActionResult> GetDistributionAsync(
        Guid dungeonRunId,
        CancellationToken cancellationToken
    )
    {
        if (
            !Request.Headers.TryGetValue("X-User-Id", out var identities)
            || identities.Count != 1
            || string.IsNullOrWhiteSpace(identities[0])
        )
        {
            return Unauthorized();
        }

        using HttpClient player = clients.CreateClient(PlayerMembershipClient);
        using var membershipRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"internal/runs/{dungeonRunId:D}/participation"
        );
        membershipRequest.Headers.Add("X-User-Id", identities[0]);
        HttpStatusCode membershipStatus;
        try
        {
            using HttpResponseMessage membership = await player.SendAsync(
                membershipRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );
            membershipStatus = membership.StatusCode;
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (membershipStatus == HttpStatusCode.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden);
        if (membershipStatus != HttpStatusCode.NoContent)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        RunRewardDistribution distribution = await sender.Send(
            new GetRunRewardDistributionQuery(dungeonRunId),
            cancellationToken
        );

        return Ok(distribution);
    }
}
