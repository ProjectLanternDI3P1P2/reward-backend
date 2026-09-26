namespace Reward.Presentation.DTO;

public sealed record AddItemToInventoryRequest(Guid ItemId, string IdempotencyKey);
