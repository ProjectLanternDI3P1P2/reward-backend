using Microsoft.EntityFrameworkCore;
using Reward.Application.Messaging;
using Reward.Domain.Entities;
using Reward.Domain.Services;
using Reward.Infrastructure.Persistence;

namespace Reward.Infrastructure.Messaging;

/// <summary>Publishes one pending outbox message with PostgreSQL worker coordination.</summary>
public sealed class OutboxDispatcher(
    RewardDbContext dbContext,
    IMessagePublisher publisher,
    IClock clock
)
{
    /// <summary>Publishes and marks the next available message, if one exists.</summary>
    public async Task<bool> DispatchNextAsync(CancellationToken cancellationToken)
    {
        // Hold a row lock through publication so parallel workers cannot send the same row together.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        OutboxMessage? message = await dbContext
            .OutboxMessages.FromSqlRaw(
                """
                SELECT *
                FROM outbox_message
                WHERE published_at_utc IS NULL
                ORDER BY attempts, occurred_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """
            )
            .SingleOrDefaultAsync(cancellationToken);

        // End the short transaction immediately when the queue is empty.
        if (message is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        try
        {
            // Rehydrate the broker-neutral envelope without coupling persistence to RabbitMQ.
            await publisher.PublishAsync(
                new MessageEnvelope(
                    message.Id,
                    message.CorrelationId,
                    message.CausationId,
                    message.Type,
                    message.Version,
                    message.OccurredAtUtc,
                    message.Producer,
                    message.Payload
                ),
                cancellationToken
            );

            // Mark success in the same row-lock transaction after RabbitMQ accepts the publish.
            message.PublishedAtUtc = clock.UtcNow;
            message.Attempts++;
            message.LastError = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Let shutdown roll the transaction back without turning cancellation into a retry.
            throw;
        }
        catch (Exception exception)
        {
            // Persist failure metadata and leave the row pending for a later worker pass.
            message.Attempts++;
            message.LastError =
                exception.Message.Length <= 2000 ? exception.Message : exception.Message[..2000];
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
            throw;
        }
    }
}
