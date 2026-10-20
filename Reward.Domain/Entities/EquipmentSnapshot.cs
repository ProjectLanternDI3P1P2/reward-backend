namespace Reward.Domain.Entities;

public sealed class EquipmentSnapshot
{
    public Guid Id { get; set; }
    public Guid InventorySnapshotId { get; set; }
    public Guid ItemInstanceId { get; set; }
    public Guid SlotId { get; set; }
    public int Quantity { get; set; }
    public InventorySnapshot InventorySnapshot { get; set; } = null!;
    public ItemInstance ItemInstance { get; set; } = null!;
    public EquipmentSlot Slot { get; set; } = null!;
}
