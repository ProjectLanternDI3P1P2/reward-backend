using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class LootRarityRuleConfiguration : IEntityTypeConfiguration<LootRarityRule>
{
    public void Configure(EntityTypeBuilder<LootRarityRule> builder)
    {
        builder.ToTable("loot_rarity_rule", table => table.HasCheckConstraint("ck_loot_rarity_rule_weight", "weight > 0"));
        builder.HasOne(x => x.LootTable).WithMany().HasForeignKey(x => x.LootTableId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Rarity).WithMany().HasForeignKey(x => x.RarityId).OnDelete(DeleteBehavior.Restrict);
    }
}
