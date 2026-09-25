using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Application.Features.PlaceholderUseCase.GetPlaceholderById;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/placeholders")]
public sealed class PlaceholderController(ISender sender) : ControllerBase
{
    [HttpGet("{placeholderId:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid placeholderId, CancellationToken cancellationToken)
    {
        var placeholder = await sender.Send(new GetPlaceholderByIdQuery(placeholderId), cancellationToken);
        return placeholder is null ? NotFound() : Ok(placeholder);
    }
}
