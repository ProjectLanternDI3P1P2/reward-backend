using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Infrastructure.Persistence.Repositories;

public sealed class RewardRepository(RewardDbContext dbContext) : IRewardRepository
{
    public Task<RewardEntity?> GetByRewardKeyForUpdateAsync(
        string rewardKey,
        CancellationToken cancellationToken
    ) =>
        dbContext
            .Rewards.FromSqlInterpolated(
                $"SELECT * FROM reward WHERE reward_key = {rewardKey} FOR UPDATE"
            )
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<RewardItem>> GetItemsByRewardIdAsync(
        Guid rewardId,
        CancellationToken cancellationToken
    ) =>
        await dbContext
            .RewardItems.Include(rewardItem => rewardItem.Item)
                .ThenInclude(item => item.Category)
            .Where(rewardItem => rewardItem.RewardId == rewardId)
            .OrderBy(rewardItem => rewardItem.CreatedAt)
            .ThenBy(rewardItem => rewardItem.Id)
            .ToListAsync(cancellationToken);
}
