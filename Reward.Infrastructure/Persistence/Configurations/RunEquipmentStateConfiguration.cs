using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RunEquipmentStateConfiguration : IEntityTypeConfiguration<RunEquipmentState>
{
    public void Configure(EntityTypeBuilder<RunEquipmentState> builder)
    {
        builder.ToTable(
            "run_equipment_state",
            table => table.HasCheckConstraint("ck_run_equipment_quantity_one", "quantity = 1")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RunInventorySessionId).HasColumnName("run_inventory_session_id");
        builder.Property(x => x.RunItemStateId).HasColumnName("run_item_state_id");
        builder.Property(x => x.SlotId).HasColumnName("slot_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => new { x.RunInventorySessionId, x.SlotId }).IsUnique();
        builder
            .HasOne(x => x.RunInventorySession)
            .WithMany()
            .HasForeignKey(x => x.RunInventorySessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.RunItemState)
            .WithMany()
            .HasForeignKey(x => x.RunItemStateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Slot)
            .WithMany()
            .HasForeignKey(x => x.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
