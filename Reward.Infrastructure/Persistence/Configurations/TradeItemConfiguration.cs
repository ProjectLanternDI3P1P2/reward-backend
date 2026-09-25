using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class TradeItemConfiguration : IEntityTypeConfiguration<TradeItem>
{
    public void Configure(EntityTypeBuilder<TradeItem> builder)
    {
        builder.ToTable("trade_item");
    }
}
