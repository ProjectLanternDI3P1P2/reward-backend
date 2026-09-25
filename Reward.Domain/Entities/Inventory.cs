namespace Reward.Domain.Entities;

public sealed class Inventory
{
    public Guid Id { get; set; }
    public Guid HeroId { get; set; }
    public int ItemCapacity { get; set; }
    public int PotionCapacity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ItemInstance> ItemInstances { get; set; } = new List<ItemInstance>();
    public ICollection<InventorySnapshot> Snapshots { get; set; } = new List<InventorySnapshot>();
}
