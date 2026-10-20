using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ItemClassTagConfiguration : IEntityTypeConfiguration<ItemClassTag>
{
    public void Configure(EntityTypeBuilder<ItemClassTag> builder)
    {
        builder.ToTable("item_class_tag");
        builder.Property(x => x.ItemId).HasColumnName("item_id");
        builder.Property(x => x.ClassTagId).HasColumnName("class_tag_id");
        builder.HasKey(x => new { x.ItemId, x.ClassTagId });
        builder
            .HasOne(x => x.Item)
            .WithMany(x => x.ClassTags)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.ClassTag)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ClassTagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
