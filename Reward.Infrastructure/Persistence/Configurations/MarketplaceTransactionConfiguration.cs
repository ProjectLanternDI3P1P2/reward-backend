using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class MarketplaceTransactionConfiguration : IEntityTypeConfiguration<MarketplaceTransaction>
{
    public void Configure(EntityTypeBuilder<MarketplaceTransaction> builder)
    {
        builder.ToTable("marketplace_transaction");
    }
}
