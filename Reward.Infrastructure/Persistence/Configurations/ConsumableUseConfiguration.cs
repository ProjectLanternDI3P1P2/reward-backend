using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class ConsumableUseConfiguration : IEntityTypeConfiguration<ConsumableUse>
{
    public void Configure(EntityTypeBuilder<ConsumableUse> builder)
    {
        builder.ToTable(
            "consumable_use",
            table =>
                table.HasCheckConstraint(
                    "ck_consumable_use_remaining_quantity",
                    "remaining_quantity >= 0"
                )
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.HeroId).HasColumnName("hero_id");
        builder.Property(x => x.ItemInstanceId).HasColumnName("item_instance_id");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
        builder.Property(x => x.RemainingQuantity).HasColumnName("remaining_quantity");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder
            .HasOne(x => x.ItemInstance)
            .WithMany()
            .HasForeignKey(x => x.ItemInstanceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
