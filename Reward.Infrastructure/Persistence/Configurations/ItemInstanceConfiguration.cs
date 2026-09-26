using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;
using Reward.Domain.Enums;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ItemInstanceConfiguration : IEntityTypeConfiguration<ItemInstance>
{
    public void Configure(EntityTypeBuilder<ItemInstance> builder)
    {
        builder.ToTable(
            "item_instance",
            table => table.HasCheckConstraint("ck_item_instance_quantity", "quantity > 0")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ItemId).HasColumnName("item_id");
        builder.Property(x => x.InventoryId).HasColumnName("inventory_id");
        builder
            .Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.ToCode(),
                value => Enum.Parse<ItemInstanceStatus>(value, ignoreCase: true)
            );
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder
            .HasOne(x => x.Item)
            .WithMany(x => x.Instances)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Inventory)
            .WithMany(x => x.ItemInstances)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
