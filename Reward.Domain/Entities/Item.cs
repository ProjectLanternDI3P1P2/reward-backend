namespace Reward.Domain.Entities;

public sealed class Item
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Guid RarityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LevelRequired { get; set; }
    public bool Stackable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Category Category { get; set; } = null!;
    public Rarity Rarity { get; set; } = null!;
    public ICollection<ItemInstance> Instances { get; set; } = new List<ItemInstance>();
    public ICollection<ItemClassTag> ClassTags { get; set; } = new List<ItemClassTag>();
    public ICollection<ItemModifier> Modifiers { get; set; } = new List<ItemModifier>();
}
