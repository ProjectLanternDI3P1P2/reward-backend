using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class OwnershipTransferConfiguration : IEntityTypeConfiguration<OwnershipTransfer>
{
    public void Configure(EntityTypeBuilder<OwnershipTransfer> builder)
    {
        builder.ToTable("ownership_transfer");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.FromOwnerId).HasColumnName("from_owner_id");
        builder.Property(x => x.ToOwnerId).HasColumnName("to_owner_id");
        builder.Property(x => x.FromInventoryId).HasColumnName("from_inventory_id");
        builder.Property(x => x.ToInventoryId).HasColumnName("to_inventory_id");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder
            .HasOne(x => x.Transaction)
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.ItemInstance)
            .WithMany()
            .HasForeignKey(x => x.ItemInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.FromInventory)
            .WithMany()
            .HasForeignKey(x => x.FromInventoryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.ToInventory)
            .WithMany()
            .HasForeignKey(x => x.ToInventoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
