using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
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

    public async Task<IReadOnlyList<RewardEntity>> GetGroupRewardsByRunAsync(
        Guid runId,
        CancellationToken cancellationToken
    ) =>
        await dbContext
            .Rewards.AsNoTracking()
            .Include(reward => reward.RewardSource)
            .Include(reward => reward.Items)
            .Where(reward =>
                reward.RunId == runId
                && reward.Status == RewardStatus.Applied
                && reward.Distribution != RewardDistribution.Individual
                && reward.CauseId != null
            )
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public void Add(RewardEntity reward) => dbContext.Rewards.Add(reward);
}
