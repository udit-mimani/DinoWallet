namespace DinoWallet.Api.Domain;

/// <summary>
/// A single debit or credit line in the double-entry ledger.
/// Every wallet event generates exactly two entries:
///   - A debit (negative Amount) on the source account
///   - A credit (positive Amount) on the destination account
/// Balance = SUM(Amount) for all entries belonging to an account.
/// </summary>
public class LedgerEntry
{
    public long Id { get; set; }

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = default!;

    public long TransactionId { get; set; }
    public Transaction Transaction { get; set; } = default!;

    /// <summary>
    /// Positive = credit (money coming in).
    /// Negative = debit (money going out).
    /// </summary>
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
