using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ClassTagConfiguration : IEntityTypeConfiguration<ClassTag>
{
    public void Configure(EntityTypeBuilder<ClassTag> builder)
    {
        builder.ToTable("class_tag");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Label).HasColumnName("label");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
