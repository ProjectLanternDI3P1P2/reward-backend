namespace Reward.Domain.Entities;

public sealed class RunItemState
{
    public Guid Id { get; set; }
    public Guid RunInventorySessionId { get; set; }
    public Guid ItemId { get; set; }
    public Guid? ItemInstanceId { get; set; }
    public int Quantity { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public RunInventorySession RunInventorySession { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public ItemInstance? ItemInstance { get; set; }
}
