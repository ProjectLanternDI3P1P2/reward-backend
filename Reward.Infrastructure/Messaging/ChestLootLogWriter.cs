using Microsoft.Extensions.DependencyInjection;
using Reward.Application.Messaging;
using Reward.Domain.Services;
using Reward.Infrastructure.Persistence;

namespace Reward.Infrastructure.Messaging;

/// <summary>Persists rejected chest logs after their command transaction has rolled back.</summary>
public sealed class ChestLootLogWriter(IServiceScopeFactory scopeFactory, IClock clock)
    : IChestLootLogWriter
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
}
