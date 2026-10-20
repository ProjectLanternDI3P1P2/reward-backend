using Reward.Application.Abstractions;

namespace Reward.Application.Features.InventoryUseCase.UnequipItem;

public sealed record UnequipItemCommand(Guid HeroId, Guid ItemInstanceId) : ICommand;
