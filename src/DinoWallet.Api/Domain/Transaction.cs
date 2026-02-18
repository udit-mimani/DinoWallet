namespace DinoWallet.Api.Domain;

public enum TransactionType
{
    /// <summary>User purchases credits via real money</summary>
    TopUp = 0,

    /// <summary>System issues free credits (referral bonus, reward, etc.)</summary>
    Bonus = 1,

    /// <summary>User spends credits within the app</summary>
    Spend = 2
}

public class Transaction
{
    public long Id { get; set; }

    public TransactionType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Caller-supplied unique key. Stored to guarantee exactly-once processing.
    /// Unique constraint enforced at DB level.
    /// </summary>
    public string IdempotencyKey { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LedgerEntry> Entries { get; set; } = new List<LedgerEntry>();
}
