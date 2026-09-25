using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.InventoryUseCase.GetHeroInventory;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/heroes/{heroId:guid}/inventory")]
public sealed class InventoryController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByHeroIdAsync(Guid heroId, CancellationToken cancellationToken)
    {
        HeroInventory? inventory = await sender.Send(new GetHeroInventoryQuery(heroId), cancellationToken);
        return inventory is null ? NotFound() : Ok(inventory);
    }
}
