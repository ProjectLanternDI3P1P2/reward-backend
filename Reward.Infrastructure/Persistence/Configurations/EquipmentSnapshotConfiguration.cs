using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class EquipmentSnapshotConfiguration : IEntityTypeConfiguration<EquipmentSnapshot>
{
    public void Configure(EntityTypeBuilder<EquipmentSnapshot> builder)
    {
        builder.ToTable(
            "equipment_snapshot",
            table => table.HasCheckConstraint("ck_equipment_snapshot_quantity_one", "quantity = 1")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InventorySnapshotId).HasColumnName("inventory_snapshot_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.SlotId).HasColumnName("slot_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder
            .HasOne(x => x.InventorySnapshot)
            .WithMany()
            .HasForeignKey(x => x.InventorySnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.ItemInstance)
            .WithMany()
            .HasForeignKey(x => x.ItemInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Slot)
            .WithMany()
            .HasForeignKey(x => x.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
