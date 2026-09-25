using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class LootTableEntryConfiguration : IEntityTypeConfiguration<LootTableEntry>
{
    public void Configure(EntityTypeBuilder<LootTableEntry> builder)
    {
        builder.ToTable("loot_table_entry", table => table.HasCheckConstraint("ck_loot_table_entry_quantities", "weight > 0 AND min_quantity > 0 AND max_quantity >= min_quantity"));
        builder.HasOne(x => x.LootRarityRule).WithMany().HasForeignKey(x => x.LootRarityRuleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
