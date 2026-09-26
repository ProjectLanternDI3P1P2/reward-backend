using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RewardConfiguration : IEntityTypeConfiguration<RewardEntity>
{
    public void Configure(EntityTypeBuilder<RewardEntity> builder)
    {
        builder.ToTable(
            "reward",
            table =>
                table.HasCheckConstraint("ck_reward_key_nonblank", "length(btrim(reward_key)) > 0")
        );
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RunId).HasColumnName("run_id");
        builder.Property(x => x.RewardSourceId).HasColumnName("reward_source_id");
        builder.Property(x => x.Type).HasColumnName("type");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.RewardKey).HasColumnName("reward_key");
        builder.Property(x => x.XpAmount).HasColumnName("xp_amount");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => x.RewardKey).IsUnique();
        builder
            .HasOne(x => x.RewardSource)
            .WithMany()
            .HasForeignKey(x => x.RewardSourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
