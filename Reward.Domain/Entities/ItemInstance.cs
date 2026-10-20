using Reward.Domain.Enums;

namespace Reward.Domain.Entities;

public sealed class ItemInstance
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid InventoryId { get; set; }
    public ItemInstanceStatus Status { get; set; }
    public string? IdempotencyKey { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Item Item { get; set; } = null!;
    public Inventory Inventory { get; set; } = null!;
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}
