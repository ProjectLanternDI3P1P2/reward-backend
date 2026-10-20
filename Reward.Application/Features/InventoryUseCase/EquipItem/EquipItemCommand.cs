using Reward.Application.Abstractions;

namespace Reward.Application.Features.InventoryUseCase.EquipItem;

public sealed record EquipItemCommand(Guid HeroId, Guid ItemInstanceId, Guid SlotId) : ICommand;
