using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Reward.Infrastructure.Messaging;

/// <summary>Continuously drains committed outbox messages to RabbitMQ.</summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Poll until host shutdown, draining bursts without an artificial delay between rows.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool dispatched;
                do
                {
                    // Give each row a fresh DbContext after its transaction completes.
                    await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                    OutboxDispatcher dispatcher =
                        scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
                    dispatched = await dispatcher.DispatchNextAsync(stoppingToken);
                } while (dispatched && !stoppingToken.IsCancellationRequested);

                // Avoid a busy loop while no committed work is available.
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host shutdown is the normal terminal condition for this worker.
            }
            catch (Exception exception)
            {
                // Keep infrastructure diagnostics local when RabbitMQ itself is unavailable.
                logger.LogError(exception, "Failed to publish a reward outbox message");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
