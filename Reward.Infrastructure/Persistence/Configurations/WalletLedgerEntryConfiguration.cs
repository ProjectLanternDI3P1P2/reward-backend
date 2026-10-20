using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class WalletLedgerEntryConfiguration : IEntityTypeConfiguration<WalletLedgerEntry>
{
    public void Configure(EntityTypeBuilder<WalletLedgerEntry> builder)
    {
        builder.ToTable("wallet_ledger_entry");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WalletId).HasColumnName("wallet_id");
        builder.Property(x => x.WalletHoldId).HasColumnName("wallet_hold_id");
        builder.Property(x => x.Delta).HasColumnName("delta").HasPrecision(18, 2);
        builder.Property(x => x.OperationKey).HasColumnName("operation_key");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder
            .HasOne(x => x.Wallet)
            .WithMany()
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.WalletHold)
            .WithMany()
            .HasForeignKey(x => x.WalletHoldId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
