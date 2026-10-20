namespace Reward.Domain.Entities;

/// <summary>Stores a broker message until RabbitMQ confirms publication.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? CausationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Producer { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = [];
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
