using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class RewardSourceConfiguration : IEntityTypeConfiguration<RewardSource>
{
    public void Configure(EntityTypeBuilder<RewardSource> builder)
    {
        builder.ToTable("reward_source");
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
