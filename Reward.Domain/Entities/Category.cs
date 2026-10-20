namespace Reward.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
