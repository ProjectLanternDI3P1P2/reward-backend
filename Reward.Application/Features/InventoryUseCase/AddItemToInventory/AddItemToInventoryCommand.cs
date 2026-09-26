using Reward.Application.Abstractions;

namespace Reward.Application.Features.InventoryUseCase.AddItemToInventory;

public sealed record AddItemToInventoryCommand(Guid HeroId, Guid ItemId, string IdempotencyKey)
    : ICommand<AddItemToInventoryResult>;
