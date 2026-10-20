using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RarityConfiguration : IEntityTypeConfiguration<Rarity>
{
    public void Configure(EntityTypeBuilder<Rarity> builder)
    {
        builder.ToTable("rarity");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Label).HasColumnName("label");
        builder.Property(x => x.Color).HasColumnName("color");
        builder.Property(x => x.Rank).HasColumnName("rank");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
