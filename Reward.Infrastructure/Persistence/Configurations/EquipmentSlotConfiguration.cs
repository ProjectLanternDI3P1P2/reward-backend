using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class EquipmentSlotConfiguration : IEntityTypeConfiguration<EquipmentSlot>
{
    public void Configure(EntityTypeBuilder<EquipmentSlot> builder)
    {
        builder.ToTable("slot");
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
