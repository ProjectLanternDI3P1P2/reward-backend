using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.InventoryUseCase.AddItemToInventory;
using Reward.Application.Features.InventoryUseCase.GetHeroInventory;
using Reward.Presentation.DTO;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/heroes/{heroId:guid}/inventory")]
public sealed class InventoryController(ISender sender) : ControllerBase
{
    [HttpPost("items")]
    public async Task<IActionResult> AddItemAsync(
        Guid heroId,
        AddItemToInventoryRequest request,
        CancellationToken cancellationToken
    )
    {
        AddItemToInventoryResult result = await sender.Send<AddItemToInventoryResult>(
            new AddItemToInventoryCommand(heroId, request.ItemId, request.IdempotencyKey),
            cancellationToken
        );

        return result.AlreadyExists
            ? Ok(result)
            : Created($"/api/v1/heroes/{heroId}/inventory", result);
    }

    [HttpGet]
    public async Task<IActionResult> GetByHeroIdAsync(
        Guid heroId,
        CancellationToken cancellationToken
    )
    {
        HeroInventory? inventory = await sender.Send(
            new GetHeroInventoryQuery(heroId),
            cancellationToken
        );
        return inventory is null ? NotFound() : Ok(inventory);
    }
}
