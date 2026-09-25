namespace Reward.Domain.Entities;

public sealed class Equipment
{
    public Guid Id { get; set; }
    public Guid HeroId { get; set; }
    public Guid ItemInstanceId { get; set; }
    public Guid SlotId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ItemInstance ItemInstance { get; set; } = null!;
    public EquipmentSlot Slot { get; set; } = null!;
}
