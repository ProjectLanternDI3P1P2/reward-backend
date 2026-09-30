using Reward.Domain.Entities;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Domain.Repositories;

public interface IRewardRepository
{
    Task<RewardEntity?> GetByKeyAsync(string rewardKey, CancellationToken cancellationToken);

    Task<RewardSource?> GetSourceByNameAsync(string name, CancellationToken cancellationToken);

    void Add(RewardEntity reward);
}
