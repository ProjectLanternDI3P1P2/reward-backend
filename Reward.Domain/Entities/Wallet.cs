namespace Reward.Domain.Entities;

public sealed class Wallet
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public decimal Balance { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
