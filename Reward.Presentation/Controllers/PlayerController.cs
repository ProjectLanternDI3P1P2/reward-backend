using Reward.Application.Features.PlayerUseCase.CreatePlayer;
using Reward.Application.Features.PlayerUseCase.GetPlayerById;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reward.Presentation.DTO;
using ILogger = Serilog.ILogger;

namespace Reward.Presentation.Controllers;

[ApiController]
[Route("api/v1/players")]
public sealed class PlayerController(IMediator mediator, ILogger logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] PlayerDto playerDto, CancellationToken cancellationToken)
    {
        logger.Information("Received request to create player {PlayerId}.", playerDto.Id);

        await mediator.Send(
            new CreatePlayerCommand(
                playerDto.Id,
                playerDto.Name,
                playerDto.Attack,
                playerDto.Health,
                playerDto.MaxHealth),
            cancellationToken);

        logger.Information("Player {PlayerId} created successfully.", playerDto.Id);
        return CreatedAtAction(nameof(GetById), new { playerId = playerDto.Id }, null);
    }

    [HttpGet("{playerId:guid}")]
    public async Task<IActionResult> GetById(Guid playerId, CancellationToken cancellationToken)
    {
        logger.Information("Received request to get player {PlayerId}.", playerId);

        var player = await mediator.Send(new GetPlayerByIdQuery(playerId), cancellationToken);

        logger.Information("Player {PlayerId} retrieved successfully.", playerId);
        return Ok(player);
    }
}
