using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RunItemStateConfiguration : IEntityTypeConfiguration<RunItemState>
{
    public void Configure(EntityTypeBuilder<RunItemState> builder)
    {
        builder.ToTable("run_item_state", table => table.HasCheckConstraint("ck_run_item_state_quantity", "quantity >= 0"));
        builder.HasOne(x => x.RunInventorySession).WithMany().HasForeignKey(x => x.RunInventorySessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ItemInstance).WithMany().HasForeignKey(x => x.ItemInstanceId).OnDelete(DeleteBehavior.Restrict);
    }
}
