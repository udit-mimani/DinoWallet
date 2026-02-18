namespace DinoWallet.Api.Domain;

public class AssetType
{
    public int Id { get; set; }

    /// <summary>Human-readable name e.g. "Gold Coins"</summary>
    public string Name { get; set; } = default!;

    /// <summary>Short ticker e.g. "GLD"</summary>
    public string Symbol { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}
