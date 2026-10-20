using MediatR;

namespace Reward.Application.Features.InventoryUseCase.GetActiveEquipment;

public sealed record GetActiveEquipmentQuery(Guid HeroId) : IRequest<ActiveEquipment?>;
