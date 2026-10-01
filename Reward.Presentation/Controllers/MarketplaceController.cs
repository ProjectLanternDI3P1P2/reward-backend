using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.MarketplaceUseCase.GetActiveMarketplaceListings;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/marketplace/listings")]
public sealed class MarketplaceController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActiveListingsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ActiveMarketplaceListing> listings = await sender.Send(
            new GetActiveMarketplaceListingsQuery(),
            cancellationToken
        );

        return Ok(listings);
    }
}
