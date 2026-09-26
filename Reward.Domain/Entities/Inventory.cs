namespace Reward.Domain.Entities;

public sealed class Inventory
{
    public const int MaximumItemSlots = 40;
    public const int MaximumPotionSlots = 20;
    public Guid Id { get; set; }
    public Guid HeroId { get; set; }
    public int ItemCapacity { get; set; }
    public int PotionCapacity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ItemInstance> ItemInstances { get; set; } = new List<ItemInstance>();
    public ICollection<InventorySnapshot> Snapshots { get; set; } = new List<InventorySnapshot>();

    public void Receive(ItemInstance itemInstance)
    {
        ArgumentNullException.ThrowIfNull(itemInstance);

        bool isPotion = string.Equals(
            itemInstance.Item.Category.Label,
            "POTION",
            StringComparison.OrdinalIgnoreCase
        );
        int occupiedSlots = ItemInstances.Count(instance =>
            string.Equals(
                instance.Item.Category.Label,
                "POTION",
                StringComparison.OrdinalIgnoreCase
            ) == isPotion
        );
        int capacity = isPotion
            ? Math.Min(PotionCapacity, MaximumPotionSlots)
            : Math.Min(ItemCapacity, MaximumItemSlots);

        if (occupiedSlots >= capacity)
        {
            string slotType = isPotion ? "potion" : "item";
            throw new InvalidOperationException($"The inventory has no available {slotType} slot.");
        }

        ItemInstances.Add(itemInstance);
    }
}
