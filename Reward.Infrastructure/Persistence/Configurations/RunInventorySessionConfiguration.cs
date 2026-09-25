using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RunInventorySessionConfiguration : IEntityTypeConfiguration<RunInventorySession>
{
    public void Configure(EntityTypeBuilder<RunInventorySession> builder)
    {
        builder.ToTable("run_inventory_session");
        builder.HasIndex(x => x.InventorySnapshotId).IsUnique();
        builder.HasOne(x => x.InventorySnapshot).WithMany().HasForeignKey(x => x.InventorySnapshotId).OnDelete(DeleteBehavior.Restrict);
    }
}
