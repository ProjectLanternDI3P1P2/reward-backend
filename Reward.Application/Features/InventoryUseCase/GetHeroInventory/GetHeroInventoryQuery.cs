using MediatR;

namespace Reward.Application.Features.InventoryUseCase.GetHeroInventory;

public sealed record GetHeroInventoryQuery(Guid HeroId) : IRequest<HeroInventory?>;
