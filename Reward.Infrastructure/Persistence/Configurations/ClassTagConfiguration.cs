using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ClassTagConfiguration : IEntityTypeConfiguration<ClassTag>
{
    public void Configure(EntityTypeBuilder<ClassTag> builder)
    {
        builder.ToTable("class_tag");
    }
}
