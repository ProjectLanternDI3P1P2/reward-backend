using Reward.Application.Features.RewardUseCase.GrantReward;

namespace Reward.Application.Features.RewardUseCase.GrantEnemyLoot;

/// <summary>
/// The loot granted for one defeated enemy. It has no hero loot when the combat was not won
/// or when no participant is eligible. A hero whose inventory cannot hold all of the loot
/// receives nothing and is listed in <paramref name="FullInventoryHeroIds"/>; the other
/// heroes are not affected.
/// </summary>
public sealed record GrantEnemyLootResult(
    Guid EnemyId,
    IReadOnlyList<GrantedHeroLoot> HeroLoots,
    IReadOnlyList<Guid> FullInventoryHeroIds
);

public sealed record GrantedHeroLoot(
    Guid HeroId,
    Guid RewardId,
    IReadOnlyList<GrantedRewardItem> Items,
    bool AlreadyGranted
);
