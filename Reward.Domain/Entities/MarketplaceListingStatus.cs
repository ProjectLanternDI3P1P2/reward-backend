namespace Reward.Domain.Entities;

/// <summary>Well-known values for <see cref="MarketplaceListing.Status"/>.</summary>
public static class MarketplaceListingStatus
{
    public const string Active = "ACTIVE";
    public const string Reserved = "RESERVED";
    public const string Sold = "SOLD";
    public const string Cancelled = "CANCELLED";
    public const string Expired = "EXPIRED";
}
