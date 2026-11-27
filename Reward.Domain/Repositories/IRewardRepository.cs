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

    /// <summary>
    /// Gets the applied rewards distributed among the participants of a run (unique or
    /// shared), with their items. Individual rewards, such as chests, are excluded.
    /// </summary>
    Task<IReadOnlyList<RewardEntity>> GetGroupRewardsByRunAsync(
        Guid runId,
        CancellationToken cancellationToken
    );

    void Add(RewardEntity reward);
}
