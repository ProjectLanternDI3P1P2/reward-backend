using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class WalletHoldConfiguration : IEntityTypeConfiguration<WalletHold>
{
    public void Configure(EntityTypeBuilder<WalletHold> builder)
    {
        builder.ToTable("wallet_hold");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WalletId).HasColumnName("wallet_id");
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");
        builder.Property(x => x.TradeId).HasColumnName("trade_id");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.HoldKey).HasColumnName("hold_key");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder
            .HasOne(x => x.Wallet)
            .WithMany()
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Transaction)
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.ClientSetNull);
        builder
            .HasOne(x => x.Trade)
            .WithMany()
            .HasForeignKey(x => x.TradeId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
