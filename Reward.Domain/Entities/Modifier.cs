namespace Reward.Domain.Entities;

public sealed class Modifier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Stat { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ItemModifier> Items { get; set; } = new List<ItemModifier>();
}
