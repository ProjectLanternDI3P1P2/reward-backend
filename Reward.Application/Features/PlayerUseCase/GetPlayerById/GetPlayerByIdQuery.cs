using MediatR;

namespace Reward.Application.Features.PlayerUseCase.GetPlayerById;

public record GetPlayerByIdQuery(Guid PlayerId) : IRequest<GetPlayerByIdResult>;
