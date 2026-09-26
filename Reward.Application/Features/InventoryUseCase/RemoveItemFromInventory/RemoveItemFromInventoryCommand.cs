using Reward.Application.Abstractions;

namespace Reward.Application.Features.InventoryUseCase.RemoveItemFromInventory;

public sealed record RemoveItemFromInventoryCommand(Guid HeroId, Guid ItemInstanceId) : ICommand;
