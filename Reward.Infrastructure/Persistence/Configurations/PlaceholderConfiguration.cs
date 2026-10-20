using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class PlaceholderConfiguration : IEntityTypeConfiguration<Placeholder>
{
    public void Configure(EntityTypeBuilder<Placeholder> builder)
    {
        builder.ToTable("placeholder");
        builder.HasKey(placeholder => placeholder.Id);
        builder.Property(placeholder => placeholder.Id).HasColumnName("id");
        builder
            .Property(placeholder => placeholder.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();
    }
}
