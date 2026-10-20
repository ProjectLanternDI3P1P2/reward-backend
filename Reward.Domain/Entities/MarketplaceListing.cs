namespace Reward.Domain.Entities;

public sealed class MarketplaceListing
{
    public Guid Id { get; set; }
    public Guid ItemInstanceId { get; set; }
    public Guid SellerId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ItemInstance ItemInstance { get; set; } = null!;
}
