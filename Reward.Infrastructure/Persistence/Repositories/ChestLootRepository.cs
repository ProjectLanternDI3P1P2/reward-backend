using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Infrastructure.Persistence.Repositories;

/// <summary>Implements chest loot persistence with PostgreSQL concurrency guarantees.</summary>
public sealed class ChestLootRepository(RewardDbContext dbContext) : IChestLootRepository
{
    public Task AcquireGenerationLockAsync(
        Guid dungeonRunId,
        Guid chestId,
        CancellationToken cancellationToken
    )
    {
        // Hash the complete business key inside PostgreSQL and hold its lock until command commit.
        string lockKey = $"{dungeonRunId:N}:{chestId:N}";
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken
        );
    }

    public Task<ChestLootGeneration?> GetGenerationAsync(
        Guid dungeonRunId,
        Guid chestId,
        CancellationToken cancellationToken
    )
    {
        // Load the entire persisted response graph so replay never consults the current table.
        return dbContext
            .ChestLootGenerations.Include(generation => generation.Reward.Items)
            .SingleOrDefaultAsync(
                generation =>
                    generation.DungeonRunId == dungeonRunId && generation.ChestId == chestId,
                cancellationToken
            );
    }

    public Task<LootTable?> GetLootTableAsync(
        int floor,
        string difficulty,
        CancellationToken cancellationToken
    )
    {
        // Match all context fields strictly; nullable legacy floors are deliberately excluded.
        return dbContext
            .LootTables.AsSplitQuery()
            .Include(table => table.RarityRules)
                .ThenInclude(rule => rule.Entries)
                    .ThenInclude(entry => entry.Item.Rarity)
            .SingleOrDefaultAsync(
                table =>
                    table.SourceType == "CHEST"
                    && table.Floor == floor
                    && table.Difficulty == difficulty,
                cancellationToken
            );
    }

    public Task<RewardSource?> GetRewardSourceAsync(
        string name,
        CancellationToken cancellationToken
    )
    {
        // Reuse the configured source row instead of creating source vocabulary per reward.
        return dbContext.RewardSources.SingleOrDefaultAsync(
            source => source.Name == name,
            cancellationToken
        );
    }

    public void AddGeneration(ChestLootGeneration generation)
    {
        // Track the complete reward/generation graph for the command transaction behavior.
        dbContext.ChestLootGenerations.Add(generation);
    }

    public void AddOutboxMessage(OutboxMessage message)
    {
        // Track broker work beside the domain state that caused it.
        dbContext.OutboxMessages.Add(message);
    }
}
