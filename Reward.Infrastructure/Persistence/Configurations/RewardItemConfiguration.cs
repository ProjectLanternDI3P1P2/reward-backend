using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RewardItemConfiguration : IEntityTypeConfiguration<RewardItem>
{
    public void Configure(EntityTypeBuilder<RewardItem> builder)
    {
        builder.ToTable(
            "reward_item",
            table => table.HasCheckConstraint("ck_reward_item_quantity", "quantity > 0")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RewardId).HasColumnName("reward_id");
        builder.Property(x => x.ItemId).HasColumnName("item_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder
            .HasOne(x => x.Reward)
            .WithMany()
            .HasForeignKey(x => x.RewardId)
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
