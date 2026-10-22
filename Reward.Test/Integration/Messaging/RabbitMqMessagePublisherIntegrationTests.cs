using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Reward.Application.Messaging;
using Reward.Contracts.Events.V1;
using Reward.Infrastructure.Messaging;

namespace Reward.Test.Integration.Messaging;

public sealed class RabbitMqMessagePublisherIntegrationTests
{
    private const string ConnectionEnvironmentVariable = "REWARD_TEST_RABBITMQ_CONNECTION";

    [Fact]
    public async Task PublishAsync_LocalChestRouting_RoutesEveryChestMessageType()
    {
        string? connectionString = Environment.GetEnvironmentVariable(
            ConnectionEnvironmentVariable
        );
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Broker verification is explicitly opt-in so the normal suite stays self-contained.
            Assert.Skip(
                $"Set {ConnectionEnvironmentVariable} to run the RabbitMQ integration test."
            );
            return;
        }

        Uri brokerUri = new(connectionString);
        string exchangeName = $"reward.chest-test.{Guid.NewGuid():N}";
        string queueName = $"reward.chest-test.{Guid.NewGuid():N}";
        RabbitMqOptions options = CreateOptions(brokerUri, exchangeName, queueName);
        var publisher = new RabbitMqMessagePublisher(
            Options.Create(options),
            NullLogger<RabbitMqMessagePublisher>.Instance
        );
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        try
        {
            string[] messageTypes =
            [
                ChestLootOutboxMessages.GeneratedEventType,
                ChestLootOutboxMessages.GenerationLogType,
                ChestLootOutboxMessages.ClaimLogType,
            ];
            foreach (string messageType in messageTypes)
            {
                await publisher.PublishAsync(
                    new MessageEnvelope(
                        Guid.NewGuid(),
                        Guid.NewGuid().ToString(),
                        null,
                        messageType,
                        1,
                        DateTimeOffset.UtcNow,
                        "reward",
                        new byte[] { 1, 2, 3 }
                    ),
                    timeout.Token
                );
            }

            await using IConnection connection = await CreateFactory(brokerUri)
                .CreateConnectionAsync(timeout.Token);
            await using IChannel channel = await connection.CreateChannelAsync(
                cancellationToken: timeout.Token
            );
            var routedTypes = new List<string>();
            for (int index = 0; index < messageTypes.Length; index++)
            {
                BasicGetResult? delivery = await channel.BasicGetAsync(
                    queueName,
                    autoAck: true,
                    timeout.Token
                );
                delivery.Should().NotBeNull();
                MessageEnvelopeMessage envelope = MessageEnvelopeMessage.Parser.ParseFrom(
                    delivery!.Body.ToArray()
                );
                routedTypes.Add(envelope.Type);
            }

            routedTypes.Should().BeEquivalentTo(messageTypes);
        }
        finally
        {
            await using IConnection cleanupConnection = await CreateFactory(brokerUri)
                .CreateConnectionAsync(CancellationToken.None);
            await using IChannel cleanupChannel = await cleanupConnection.CreateChannelAsync(
                cancellationToken: CancellationToken.None
            );
            await cleanupChannel.QueueDeleteAsync(
                queueName,
                cancellationToken: CancellationToken.None
            );
            await cleanupChannel.ExchangeDeleteAsync(
                exchangeName,
                cancellationToken: CancellationToken.None
            );
        }
    }

    private static RabbitMqOptions CreateOptions(Uri uri, string exchangeName, string queueName)
    {
        string[] credentials = Uri.UnescapeDataString(uri.UserInfo).Split(':', 2);
        return new RabbitMqOptions
        {
            Enabled = true,
            HostName = uri.Host,
            Port = uri.Port,
            VirtualHost = Uri.UnescapeDataString(uri.AbsolutePath),
            UserName = credentials[0],
            Password = credentials[1],
            ExchangeName = exchangeName,
            ChestRoutingQueueName = queueName,
        };
    }

    private static ConnectionFactory CreateFactory(Uri uri)
    {
        var factory = new ConnectionFactory();
        factory.Uri = uri;
        return factory;
    }
}
