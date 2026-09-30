using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Reward.Application.Messaging;
using Reward.Contracts.Events.V1;

namespace Reward.Infrastructure.Messaging;

/// <summary>RabbitMQ adapter for the broker-independent message publisher seam.</summary>
public sealed class RabbitMqMessagePublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqMessagePublisher> logger
) : IMessagePublisher
{
    /// <summary>Publishes one protobuf envelope and waits for broker confirmation.</summary>
    public async Task PublishAsync(MessageEnvelope message, CancellationToken cancellationToken)
    {
        // Build the connection from validated settings owned by the messaging registration.
        RabbitMqOptions settings = options.Value;
        ConnectionFactory factory = new()
        {
            HostName = settings.HostName,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            UserName = settings.UserName,
            Password = settings.Password,
        };

        // Open short-lived resources so a failed connection cannot poison later worker attempts.
        await using IConnection connection = await factory.CreateConnectionAsync(cancellationToken);
        // Track mandatory returns and broker acknowledgements for this exact publication.
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );
        await using IChannel channel = await connection.CreateChannelAsync(
            channelOptions,
            cancellationToken: cancellationToken
        );
        // Ensure the durable topic exchange exists before mandatory publication.
        await channel.ExchangeDeclareAsync(
            settings.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken
        );

        // Serialize the transport-neutral envelope defined in the public event contract.
        MessageEnvelopeMessage envelope = new()
        {
            MessageId = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId,
            CausationId = message.CausationId ?? string.Empty,
            Type = message.Type,
            Version = message.Version,
            OccurredAtUtc = message.OccurredAtUtc.ToString("O"),
            Producer = message.Producer,
            Payload = ByteString.CopyFrom(message.Payload.Span),
        };

        // Mark the protobuf envelope durable and identifiable at the AMQP layer.
        BasicProperties properties = new()
        {
            Persistent = true,
            ContentType = "application/x-protobuf",
            MessageId = message.MessageId.ToString(),
        };

        // Awaiting this call now includes publisher confirm and mandatory return tracking.
        await channel.BasicPublishAsync(
            settings.ExchangeName,
            $"{message.Type}.v{message.Version}",
            mandatory: true,
            basicProperties: properties,
            body: envelope.ToByteArray(),
            cancellationToken: cancellationToken
        );

        // Log success only after BasicPublishAsync has received its broker confirmation.
        logger.LogInformation(
            "Published integration message {MessageType} with id {MessageId}",
            message.Type,
            message.MessageId
        );
    }
}
