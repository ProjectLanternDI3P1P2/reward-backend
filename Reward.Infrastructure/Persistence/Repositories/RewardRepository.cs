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

    public async Task LockKeyAsync(string rewardKey, CancellationToken cancellationToken)
    {
        // Transaction-scoped advisory lock: released automatically on commit or rollback.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({rewardKey}, 0))",
            cancellationToken
        );
    }

    public void Add(RewardEntity reward) => dbContext.Rewards.Add(reward);

    public Task<RewardEntity?> GetChestRewardAsync(
        Guid dungeonRunId,
        Guid chestId,
        CancellationToken cancellationToken
    ) =>
        dbContext
            .Rewards.Include(reward => reward.Items)
                .ThenInclude(rewardItem => rewardItem.Item)
                    .ThenInclude(item => item.Category)
            .Where(reward =>
                dbContext.ChestLootGenerations.Any(generation =>
                    generation.RewardId == reward.Id
                    && generation.DungeonRunId == dungeonRunId
                    && generation.ChestId == chestId
                )
            )
            .SingleOrDefaultAsync(cancellationToken);
}
