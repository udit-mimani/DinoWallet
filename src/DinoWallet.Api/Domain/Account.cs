namespace DinoWallet.Api.Domain;

public enum AccountType
{
    User = 0,
    System = 1
}

public class Account
{
    public Guid Id { get; set; }

    /// <summary>Display name — user name or system account label</summary>
    public string Name { get; set; } = default!;

    public AccountType Type { get; set; }

    public int AssetTypeId { get; set; }
    public AssetType AssetType { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}
