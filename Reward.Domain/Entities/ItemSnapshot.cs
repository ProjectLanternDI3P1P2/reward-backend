namespace Reward.Domain.Entities;

public sealed class ItemSnapshot
{
    public Guid Id { get; set; }
    public Guid InventorySnapshotId { get; set; }
    public Guid ItemInstanceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Quantity { get; set; }
    public InventorySnapshot InventorySnapshot { get; set; } = null!;
    public ItemInstance ItemInstance { get; set; } = null!;
}
