using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable(
            "inventory",
            table =>
                table.HasCheckConstraint(
                    "ck_inventory_capacities",
                    "item_capacity > 0 AND potion_capacity > 0"
                )
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.HeroId).HasColumnName("hero_id");
        builder.Property(x => x.ItemCapacity).HasColumnName("item_capacity");
        builder.Property(x => x.PotionCapacity).HasColumnName("potion_capacity");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
