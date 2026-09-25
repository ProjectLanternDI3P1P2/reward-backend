using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RewardEntity = Reward.Domain.Entities.Reward;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RewardConfiguration : IEntityTypeConfiguration<RewardEntity>
{
    public void Configure(EntityTypeBuilder<RewardEntity> builder)
    {
        builder.ToTable("reward", table => table.HasCheckConstraint("ck_reward_key_nonblank", "length(btrim(reward_key)) > 0"));
        builder.HasIndex(x => x.RewardKey).IsUnique();
        builder.HasOne(x => x.RewardSource).WithMany().HasForeignKey(x => x.RewardSourceId).OnDelete(DeleteBehavior.Restrict);
    }
}
