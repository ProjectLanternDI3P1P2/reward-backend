namespace Reward.Domain.Entities;

public sealed class MarketplaceTransaction
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Amount { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public MarketplaceListing Listing { get; set; } = null!;
    public MarketplaceReservation? Reservation { get; set; }
}
