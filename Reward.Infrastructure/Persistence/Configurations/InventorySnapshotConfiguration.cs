using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class InventorySnapshotConfiguration : IEntityTypeConfiguration<InventorySnapshot>
{
    public void Configure(EntityTypeBuilder<InventorySnapshot> builder)
    {
        builder.ToTable("inventory_snapshot");
        builder.HasIndex(x => new { x.InventoryId, x.RunId }).IsUnique();
        builder.HasOne(x => x.Inventory).WithMany(x => x.Snapshots).HasForeignKey(x => x.InventoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
