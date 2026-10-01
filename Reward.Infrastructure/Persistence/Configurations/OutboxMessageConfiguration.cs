using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

/// <summary>Maps durable integration messages awaiting RabbitMQ publication.</summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // Prevent invalid retry or version state from entering the durable queue.
        builder.ToTable(
            "outbox_message",
            table =>
            {
                table.HasCheckConstraint("ck_outbox_message_version", "version > 0");
                table.HasCheckConstraint("ck_outbox_message_attempts", "attempts >= 0");
            }
        );

        // Map the broker-neutral envelope and delivery bookkeeping.
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        builder.Property(x => x.CausationId).HasColumnName("causation_id");
        builder.Property(x => x.Type).HasColumnName("type");
        builder.Property(x => x.Version).HasColumnName("version");
        builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        builder.Property(x => x.Producer).HasColumnName("producer");
        builder.Property(x => x.Payload).HasColumnName("payload");
        builder.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        builder.Property(x => x.Attempts).HasColumnName("attempts");
        builder.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000);

        // Keep the worker's pending scan narrow as the history grows.
        builder
            .HasIndex(x => new
            {
                x.PublishedAtUtc,
                x.Attempts,
                x.OccurredAtUtc,
            })
            .HasFilter("published_at_utc IS NULL");
    }
}
