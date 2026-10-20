using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reward.Application.Messaging;

namespace Reward.Infrastructure.Messaging;

/// <summary>Registers broker adapters and the durable outbox publisher.</summary>
public static class MessagingServiceRegistration
{
    /// <summary>Adds messaging services and starts the worker when RabbitMQ is enabled.</summary>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Bind and validate broker settings once during service startup.
        RabbitMqOptions options =
            configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();
        Validate(options);
        services.AddSingleton(Options.Create(options));
        // Register durable writers independently from whether publishing is currently enabled.
        services.AddScoped<OutboxDispatcher>();
        services.AddSingleton<IChestLootLogWriter, ChestLootLogWriter>();

        // Preserve pending outbox rows when RabbitMQ is intentionally disabled.
        if (!options.Enabled)
        {
            return services.AddSingleton<IMessagePublisher, NoOpMessagePublisher>();
        }

        // Start the durable publisher only when a real broker adapter is configured.
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddHostedService<OutboxPublisherWorker>();
        return services;
    }

    private static void Validate(RabbitMqOptions options)
    {
        if (options.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException("RabbitMq:Port must be between 1 and 65535.");
        }

        if (
            string.IsNullOrWhiteSpace(options.HostName)
            || string.IsNullOrWhiteSpace(options.ExchangeName)
        )
        {
            throw new InvalidOperationException(
                "RabbitMq:HostName and RabbitMq:ExchangeName are required."
            );
        }
    }
}
