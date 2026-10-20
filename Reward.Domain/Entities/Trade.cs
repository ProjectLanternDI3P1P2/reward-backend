namespace Reward.Domain.Entities;

public sealed class Trade
{
    public Guid Id { get; set; }
    public Guid InitiatorId { get; set; }
    public Guid CounterpartyId { get; set; }
    public decimal CashAdjustment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
