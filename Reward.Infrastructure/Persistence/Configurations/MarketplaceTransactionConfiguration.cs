using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class MarketplaceTransactionConfiguration
    : IEntityTypeConfiguration<MarketplaceTransaction>
{
    public void Configure(EntityTypeBuilder<MarketplaceTransaction> builder)
    {
        builder.ToTable("marketplace_transaction");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ListingId).HasColumnName("listing_id");
        builder.Property(x => x.ReservationId).HasColumnName("reservation_id");
        builder.Property(x => x.BuyerId).HasColumnName("buyer_id");
        builder.Property(x => x.SellerId).HasColumnName("seller_id");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder
            .HasOne(x => x.Listing)
            .WithMany()
            .HasForeignKey(x => x.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Reservation)
            .WithMany()
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
