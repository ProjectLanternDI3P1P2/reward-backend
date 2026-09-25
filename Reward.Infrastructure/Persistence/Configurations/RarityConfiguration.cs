using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RarityConfiguration : IEntityTypeConfiguration<Rarity>
{
    public void Configure(EntityTypeBuilder<Rarity> builder)
    {
        builder.ToTable("rarity");
    }
}
