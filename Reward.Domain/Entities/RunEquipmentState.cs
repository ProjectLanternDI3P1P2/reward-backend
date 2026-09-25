namespace Reward.Domain.Entities;

public sealed class RunEquipmentState
{
    public Guid Id { get; set; }
    public Guid RunInventorySessionId { get; set; }
    public Guid RunItemStateId { get; set; }
    public Guid SlotId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public RunInventorySession RunInventorySession { get; set; } = null!;
    public RunItemState RunItemState { get; set; } = null!;
    public EquipmentSlot Slot { get; set; } = null!;
}
