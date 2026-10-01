namespace Reward.Application.Features.RewardUseCase.GrantReward;

public sealed record GrantRewardResult(
    Guid RewardId,
    IReadOnlyList<GrantedRewardItem> Items,
    bool AlreadyGranted
);

public sealed record GrantedRewardItem(Guid ItemId, Guid? ItemInstanceId, int Quantity);
