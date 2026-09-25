namespace Reward.Domain.Entities;

public sealed class WalletLedgerEntry
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid? WalletHoldId { get; set; }
    public decimal Delta { get; set; }
    public string OperationKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public WalletHold? WalletHold { get; set; }
}
