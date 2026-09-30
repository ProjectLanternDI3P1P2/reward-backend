using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

/// <summary>Maps weighted rarity rules to their table and catalogue rarity.</summary>
public sealed class LootRarityRuleConfiguration : IEntityTypeConfiguration<LootRarityRule>
{
    public void Configure(EntityTypeBuilder<LootRarityRule> builder)
    {
        // Require positive rarity weights at the persistence boundary.
        builder.ToTable(
            "loot_rarity_rule",
            table => table.HasCheckConstraint("ck_loot_rarity_rule_weight", "weight > 0")
        );
        // Map identifiers and weight using the shared database naming convention.
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LootTableId).HasColumnName("loot_table_id");
        builder.Property(x => x.RarityId).HasColumnName("rarity_id");
        builder.Property(x => x.Weight).HasColumnName("weight");
        // Keep rules owned by their table and catalogue rarities protected from cascade deletion.
        builder
            .HasOne(x => x.LootTable)
            .WithMany(x => x.RarityRules)
            .HasForeignKey(x => x.LootTableId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Rarity)
            .WithMany()
            .HasForeignKey(x => x.RarityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
