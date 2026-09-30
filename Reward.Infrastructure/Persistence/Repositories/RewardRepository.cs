using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Infrastructure.Persistence.Repositories;

public sealed class RewardRepository(RewardDbContext dbContext) : IRewardRepository
{
    public Task<RewardEntity?> GetByKeyAsync(
        string rewardKey,
        CancellationToken cancellationToken
    ) =>
        dbContext
            .Rewards.Include(reward => reward.Items)
            .SingleOrDefaultAsync(reward => reward.RewardKey == rewardKey, cancellationToken);

    public Task<RewardSource?> GetSourceByNameAsync(
        string name,
        CancellationToken cancellationToken
    ) =>
        dbContext.RewardSources.SingleOrDefaultAsync(
            source => source.Name == name,
            cancellationToken
        );

    public void Add(RewardEntity reward) => dbContext.Rewards.Add(reward);
}
