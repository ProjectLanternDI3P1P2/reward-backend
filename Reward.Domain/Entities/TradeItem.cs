namespace Reward.Domain.Entities;

public sealed class TradeItem
{
    public Guid Id { get; set; }
    public Guid TradeId { get; set; }
    public Guid ItemInstanceId { get; set; }
    public int Quantity { get; set; }
    public string Side { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Trade Trade { get; set; } = null!;
    public ItemInstance ItemInstance { get; set; } = null!;
}
