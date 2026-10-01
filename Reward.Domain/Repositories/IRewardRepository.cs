using Reward.Domain.Entities;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Domain.Repositories;

public interface IRewardRepository
{
    Task<RewardEntity?> GetByKeyAsync(string rewardKey, CancellationToken cancellationToken);

    Task<RewardSource?> GetSourceByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Serializes operations sharing <paramref name="rewardKey"/> until the current
    /// transaction ends.
    /// </summary>
    Task LockKeyAsync(string rewardKey, CancellationToken cancellationToken);

    void Add(RewardEntity reward);

    /// <summary>
    /// Gets the reward generated for a chest of a dungeon run, with the items and
    /// categories needed to transfer it to an inventory.
    /// </summary>
    Task<RewardEntity?> GetChestRewardAsync(
        Guid dungeonRunId,
        Guid chestId,
        CancellationToken cancellationToken
    );
}
