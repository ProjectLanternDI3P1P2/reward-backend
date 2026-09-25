using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ItemSnapshotConfiguration : IEntityTypeConfiguration<ItemSnapshot>
{
    public void Configure(EntityTypeBuilder<ItemSnapshot> builder)
    {
        builder.ToTable("item_snapshot", table => table.HasCheckConstraint("ck_item_snapshot_quantity", "quantity >= 0"));
        builder.HasOne(x => x.InventorySnapshot).WithMany(x => x.ItemSnapshots).HasForeignKey(x => x.InventorySnapshotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ItemInstance).WithMany().HasForeignKey(x => x.ItemInstanceId).OnDelete(DeleteBehavior.Restrict);
    }
}
