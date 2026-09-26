using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class LootRarityRuleConfiguration : IEntityTypeConfiguration<LootRarityRule>
{
    public void Configure(EntityTypeBuilder<LootRarityRule> builder)
    {
        builder.ToTable(
            "loot_rarity_rule",
            table => table.HasCheckConstraint("ck_loot_rarity_rule_weight", "weight > 0")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LootTableId).HasColumnName("loot_table_id");
        builder.Property(x => x.RarityId).HasColumnName("rarity_id");
        builder.Property(x => x.Weight).HasColumnName("weight");
        builder
            .HasOne(x => x.LootTable)
            .WithMany()
            .HasForeignKey(x => x.LootTableId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Rarity)
            .WithMany()
            .HasForeignKey(x => x.RarityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
