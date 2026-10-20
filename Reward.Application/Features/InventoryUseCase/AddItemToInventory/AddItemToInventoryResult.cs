namespace Reward.Application.Features.InventoryUseCase.AddItemToInventory;

public sealed record AddItemToInventoryResult(Guid ItemInstanceId, bool AlreadyExists);
