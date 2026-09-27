using Reward.Application.Abstractions;

namespace Reward.Application.Features.InventoryUseCase.ConsumeConsumable;

public sealed record ConsumeConsumableCommand(
    Guid HeroId,
    Guid ItemInstanceId,
    string IdempotencyKey
) : ICommand<ConsumeConsumableResult>;
