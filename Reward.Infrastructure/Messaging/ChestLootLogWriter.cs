using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reward.Application.Messaging;
using Reward.Domain.Services;
using Reward.Infrastructure.Persistence;

namespace Reward.Infrastructure.Messaging;

/// <summary>Persists chest failure logs after their command transaction has rolled back.</summary>
public sealed class ChestLootLogWriter(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<ChestLootLogWriter> logger
) : IChestLootLogWriter
{
    public async Task WriteRejectedAsync(
        RejectedChestLootLog log,
        CancellationToken cancellationToken
    )
    {
        // Use a new scope so rejection logging is independent from the failed command transaction.
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();

        // Store the structured log for the same Rabbit worker used by successful commands.
        dbContext.OutboxMessages.Add(ChestLootOutboxMessages.CreateRejectedLog(log, clock.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task TryWriteCapacityRefusedAsync(
        RefusedChestLootClaimLog log,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // The command transaction has already rolled back. A new scope makes this log durable
            // without accidentally committing any partially tracked inventory or reward changes.
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
            dbContext.OutboxMessages.Add(
                ChestLootOutboxMessages.CreateClaimLog(
                    log.HeroId,
                    log.DungeonRunId,
                    log.ChestId,
                    "CAPACITY_REFUSED",
                    0,
                    clock.UtcNow
                )
            );
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not persist CAPACITY_REFUSED claim log for chest {ChestId}",
                log.ChestId
            );
        }
    }
}
