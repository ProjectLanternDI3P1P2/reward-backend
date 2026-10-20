using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable(
            "equipment",
            table => table.HasCheckConstraint("ck_equipment_quantity_one", "quantity = 1")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.HeroId).HasColumnName("hero_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.SlotId).HasColumnName("slot_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => new { x.HeroId, x.SlotId }).IsUnique();
        builder
            .HasOne(x => x.ItemInstance)
            .WithMany(x => x.Equipment)
            .HasForeignKey(x => x.ItemInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Slot)
            .WithMany()
            .HasForeignKey(x => x.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
