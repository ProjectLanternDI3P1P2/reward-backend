namespace Reward.Domain.Entities;

public sealed class LootTable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
