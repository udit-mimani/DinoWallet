namespace DinoWallet.Api.DTOs.Responses;

public class TransactionResponse
{
    public long TransactionId { get; set; }
    public string Type { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public List<LedgerEntryResponse> Entries { get; set; } = new();
}

public class LedgerEntryResponse
{
    public long Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = default!;
    public decimal Amount { get; set; }
    public long TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BalanceResponse
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = default!;
    public string AssetType { get; set; } = default!;
    public string AssetSymbol { get; set; } = default!;
    public decimal Balance { get; set; }
}

public class AccountResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string AssetType { get; set; } = default!;
    public string AssetSymbol { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class LedgerPageResponse
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = default!;
    public decimal RunningBalance { get; set; }
    public int TotalEntries { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<LedgerEntryResponse> Entries { get; set; } = new();
}

public class ErrorResponse
{
    public string Error { get; set; } = default!;
    public string? Detail { get; set; }
}
