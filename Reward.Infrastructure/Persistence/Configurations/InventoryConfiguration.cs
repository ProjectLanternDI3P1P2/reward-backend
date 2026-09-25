using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventory", table => table.HasCheckConstraint("ck_inventory_capacities", "item_capacity > 0 AND potion_capacity > 0"));
        builder.HasMany(x => x.ItemInstances).WithOne(x => x.Inventory).HasForeignKey(x => x.InventoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Snapshots).WithOne(x => x.Inventory).HasForeignKey(x => x.InventoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
