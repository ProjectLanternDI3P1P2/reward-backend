using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class WalletHoldConfiguration : IEntityTypeConfiguration<WalletHold>
{
    public void Configure(EntityTypeBuilder<WalletHold> builder)
    {
        builder.ToTable("wallet_hold");
    }
}
