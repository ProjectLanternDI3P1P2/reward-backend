using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ItemSnapshotConfiguration : IEntityTypeConfiguration<ItemSnapshot>
{
    public void Configure(EntityTypeBuilder<ItemSnapshot> builder)
    {
        builder.ToTable(
            "item_snapshot",
            table => table.HasCheckConstraint("ck_item_snapshot_quantity", "quantity >= 0")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InventorySnapshotId).HasColumnName("inventory_snapshot_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder
            .HasOne(x => x.InventorySnapshot)
            .WithMany(x => x.ItemSnapshots)
            .HasForeignKey(x => x.InventorySnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.ItemInstance)
            .WithMany()
            .HasForeignKey(x => x.ItemInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
