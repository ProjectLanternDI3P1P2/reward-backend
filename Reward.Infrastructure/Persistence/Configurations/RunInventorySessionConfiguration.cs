using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RunInventorySessionConfiguration : IEntityTypeConfiguration<RunInventorySession>
{
    public void Configure(EntityTypeBuilder<RunInventorySession> builder)
    {
        builder.ToTable("run_inventory_session");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InventorySnapshotId).HasColumnName("inventory_snapshot_id");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.Phase).HasColumnName("phase");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.ClosedAt).HasColumnName("closed_at");
        builder.HasIndex(x => x.InventorySnapshotId).IsUnique();
        builder
            .HasOne(x => x.InventorySnapshot)
            .WithMany()
            .HasForeignKey(x => x.InventorySnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
