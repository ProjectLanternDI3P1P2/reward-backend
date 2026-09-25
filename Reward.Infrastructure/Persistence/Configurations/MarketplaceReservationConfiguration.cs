using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class MarketplaceReservationConfiguration : IEntityTypeConfiguration<MarketplaceReservation>
{
    public void Configure(EntityTypeBuilder<MarketplaceReservation> builder)
    {
        builder.ToTable("marketplace_reservation");
    }
}
