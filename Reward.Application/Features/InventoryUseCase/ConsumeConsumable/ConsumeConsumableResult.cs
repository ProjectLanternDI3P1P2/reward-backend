namespace Reward.Application.Features.InventoryUseCase.ConsumeConsumable;

public sealed record ConsumeConsumableResult(
    Guid ItemInstanceId,
    int RemainingQuantity,
    bool AlreadyConsumed
);
