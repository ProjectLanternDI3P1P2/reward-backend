namespace Reward.Domain.Entities;

public sealed class WalletHold
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid? TradeId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string HoldKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public MarketplaceTransaction? Transaction { get; set; }
    public Trade? Trade { get; set; }
}
