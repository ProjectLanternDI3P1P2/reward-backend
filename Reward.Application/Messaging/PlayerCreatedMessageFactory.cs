using Reward.Contracts.Events.V1;
using Google.Protobuf;

namespace Reward.Application.Messaging;

public static class PlayerCreatedMessageFactory
{
    public const string MessageType = "reward.player.created";
    public const int Version = 1;

    public static MessageEnvelope Create(Guid playerId, string name, int attack, int health, int maxHealth)
    {
        PlayerCreated payload = new()
        {
            PlayerId = playerId.ToString(),
            Name = name,
            Attack = attack,
            Health = health,
            MaxHealth = maxHealth
        };

        Guid messageId = Guid.NewGuid();
        return new MessageEnvelope(messageId, messageId.ToString(), null, MessageType, Version,
            DateTimeOffset.UtcNow, "reward", payload.ToByteArray());
    }
}
