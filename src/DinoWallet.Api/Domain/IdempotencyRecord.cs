namespace DinoWallet.Api.Domain;

/// <summary>
/// Persisted record of every processed idempotency key.
/// If an incoming request key already exists here, we return the cached result
/// without re-executing the transaction logic.
/// </summary>
public class IdempotencyRecord
{
    /// <summary>Caller-supplied unique key (UUID string recommended)</summary>
    public string Key { get; set; } = default!;

    /// <summary>The transaction that was created for this key</summary>
    public long TransactionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
