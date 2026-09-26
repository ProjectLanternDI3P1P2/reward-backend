using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RunItemStateConfiguration : IEntityTypeConfiguration<RunItemState>
{
    public void Configure(EntityTypeBuilder<RunItemState> builder)
    {
        builder.ToTable(
            "run_item_state",
            table => table.HasCheckConstraint("ck_run_item_state_quantity", "quantity >= 0")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RunInventorySessionId).HasColumnName("run_inventory_session_id");
        builder.Property(x => x.ItemId).HasColumnName("item_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.State).HasColumnName("state");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder
            .HasOne(x => x.RunInventorySession)
            .WithMany()
            .HasForeignKey(x => x.RunInventorySessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.ItemInstance)
            .WithMany()
            .HasForeignKey(x => x.ItemInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
