namespace Reward.Domain.Entities;

public sealed class Rarity
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Rank { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
