using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ItemInstanceConfiguration : IEntityTypeConfiguration<ItemInstance>
{
    public void Configure(EntityTypeBuilder<ItemInstance> builder)
    {
        builder.ToTable("item_instance", table => table.HasCheckConstraint("ck_item_instance_quantity", "quantity > 0"));
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasOne(x => x.Item).WithMany(x => x.Instances).HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
