using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

/// <summary>Maps reward item quantities and their immutable display snapshots.</summary>
public sealed class RewardItemConfiguration : IEntityTypeConfiguration<RewardItem>
{
    public void Configure(EntityTypeBuilder<RewardItem> builder)
    {
        // Keep every aggregated reward quantity strictly positive.
        builder.ToTable(
            "reward_item",
            table => table.HasCheckConstraint("ck_reward_item_quantity", "quantity > 0")
        );
        // Persist display snapshots beside stable item identity and quantity.
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RewardId).HasColumnName("reward_id");
        builder.Property(x => x.ItemId).HasColumnName("item_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.ItemNameSnapshot).HasColumnName("item_name_snapshot");
        builder.Property(x => x.ItemRaritySnapshot).HasColumnName("item_rarity_snapshot");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        // Keep reward items owned by rewards while protecting catalogue and instance references.
        builder
            .HasOne(x => x.Reward)
            .WithMany(x => x.Items)
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
            // A rewarded stack may later be consumed or removed; the reward keeps its item and quantity.
            .OnDelete(DeleteBehavior.SetNull);
    }
}
