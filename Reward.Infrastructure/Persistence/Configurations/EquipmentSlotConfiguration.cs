using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class EquipmentSlotConfiguration : IEntityTypeConfiguration<EquipmentSlot>
{
    public void Configure(EntityTypeBuilder<EquipmentSlot> builder)
    {
        builder.ToTable("slot");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
