namespace Reward.Application.Features.ChestUseCase.ClaimChestContents;

public sealed record ClaimChestContentsResult(
    Guid ChestId,
    string State,
    IReadOnlyList<ClaimedChestItem> Items,
    bool AlreadyEmpty
);

public sealed record ClaimedChestItem(Guid ItemInstanceId, Guid ItemId, int Quantity);
