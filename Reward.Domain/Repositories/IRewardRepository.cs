using Reward.Domain.Entities;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Domain.Repositories;

public interface IRewardRepository
{
    Task<RewardEntity?> GetByRewardKeyForUpdateAsync(
        string rewardKey,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<RewardItem>> GetItemsByRewardIdAsync(
        Guid rewardId,
        CancellationToken cancellationToken
    );
}
