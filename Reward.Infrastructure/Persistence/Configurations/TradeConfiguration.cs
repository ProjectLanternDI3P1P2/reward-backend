using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("trade");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InitiatorId).HasColumnName("initiator_id");
        builder.Property(x => x.CounterpartyId).HasColumnName("counterparty_id");
        builder
            .Property(x => x.CashAdjustment)
            .HasColumnName("cash_adjustment")
            .HasPrecision(18, 2);
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
