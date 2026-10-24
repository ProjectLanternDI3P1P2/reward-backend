using Reward.Application.Abstractions;
using Reward.Domain.Enums;

namespace Reward.Application.Features.RewardUseCase.GrantReward;

/// <summary>
/// Grants the rewards earned by one hero for one game event (a combat, a boss or a chest).
/// The same reward is credited to every eligible participant, one command per hero.
/// </summary>
public sealed record GrantRewardCommand(
    Guid HeroId,
    Guid RunId,
    RewardSourceType SourceType,
    Guid CauseId,
    IReadOnlyList<GrantRewardItem> Items,
    Guid? PlayerId = null
) : ICommand<GrantRewardResult>;

public sealed record GrantRewardItem(Guid ItemId, int Quantity);
