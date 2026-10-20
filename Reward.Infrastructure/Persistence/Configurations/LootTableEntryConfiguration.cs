using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

/// <summary>Maps weighted loot entries and inclusive quantity bounds.</summary>
public sealed class LootTableEntryConfiguration : IEntityTypeConfiguration<LootTableEntry>
{
    public void Configure(EntityTypeBuilder<LootTableEntry> builder)
    {
        // Reject non-positive weights and invalid inclusive quantity ranges.
        builder.ToTable(
            "loot_table_entry",
            table =>
                table.HasCheckConstraint(
                    "ck_loot_table_entry_quantities",
                    "weight > 0 AND min_quantity > 0 AND max_quantity >= min_quantity"
                )
        );
        // Map weighted entry fields to their existing schema names.
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LootRarityRuleId).HasColumnName("loot_rarity_rule_id");
        builder.Property(x => x.ItemId).HasColumnName("item_id");
        builder.Property(x => x.Weight).HasColumnName("weight");
        builder.Property(x => x.MinQuantity).HasColumnName("min_quantity");
        builder.Property(x => x.MaxQuantity).HasColumnName("max_quantity");
        // Cascade rule-owned entries while protecting referenced catalogue items.
        builder
            .HasOne(x => x.LootRarityRule)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.LootRarityRuleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
