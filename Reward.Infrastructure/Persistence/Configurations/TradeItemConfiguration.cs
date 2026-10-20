using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class TradeItemConfiguration : IEntityTypeConfiguration<TradeItem>
{
    public void Configure(EntityTypeBuilder<TradeItem> builder)
    {
        builder.ToTable("trade_item");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TradeId).HasColumnName("trade_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.Side).HasColumnName("side");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder
            .HasOne(x => x.Trade)
            .WithMany()
            .HasForeignKey(x => x.TradeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.ItemInstance)
            .WithMany()
            .HasForeignKey(x => x.ItemInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
