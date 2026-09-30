namespace Reward.Application.Features.RewardUseCase.AwardUniqueGroupReward;

public sealed record AwardUniqueGroupRewardResult(
    Guid RewardId,
    Guid RecipientHeroId,
    Guid ItemId,
    Guid? ItemInstanceId,
    int Quantity,
    bool AlreadyAwarded
);
