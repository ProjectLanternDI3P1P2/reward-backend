using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RewardItemConfiguration : IEntityTypeConfiguration<RewardItem>
{
    public void Configure(EntityTypeBuilder<RewardItem> builder)
    {
        builder.ToTable("reward_item", table => table.HasCheckConstraint("ck_reward_item_quantity", "quantity > 0"));
        builder.HasOne(x => x.Reward).WithMany().HasForeignKey(x => x.RewardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ItemInstance).WithMany().HasForeignKey(x => x.ItemInstanceId).OnDelete(DeleteBehavior.Restrict);
    }
}
