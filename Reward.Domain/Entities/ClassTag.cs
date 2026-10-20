namespace Reward.Domain.Entities;

public sealed class ClassTag
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ItemClassTag> Items { get; set; } = new List<ItemClassTag>();
}
