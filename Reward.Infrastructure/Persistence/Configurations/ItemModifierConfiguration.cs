using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ItemModifierConfiguration : IEntityTypeConfiguration<ItemModifier>
{
    public void Configure(EntityTypeBuilder<ItemModifier> builder)
    {
        builder.ToTable("item_modifier");
        builder.HasKey(x => new { x.ItemId, x.ModifierId });
        builder.HasOne(x => x.Item).WithMany(x => x.Modifiers).HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Modifier).WithMany(x => x.Items).HasForeignKey(x => x.ModifierId).OnDelete(DeleteBehavior.Cascade);
    }
}
