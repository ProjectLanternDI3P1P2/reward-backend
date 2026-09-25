namespace Reward.Domain.Entities;

public sealed class InventorySnapshot
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public Guid RunId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Inventory Inventory { get; set; } = null!;
    public ICollection<ItemSnapshot> ItemSnapshots { get; set; } = new List<ItemSnapshot>();
}
