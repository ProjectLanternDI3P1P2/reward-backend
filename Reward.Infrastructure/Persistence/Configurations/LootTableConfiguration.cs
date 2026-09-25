using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class LootTableConfiguration : IEntityTypeConfiguration<LootTable>
{
    public void Configure(EntityTypeBuilder<LootTable> builder)
    {
        builder.ToTable("loot_table");
    }
}
