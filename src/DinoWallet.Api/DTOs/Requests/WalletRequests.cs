using System.ComponentModel.DataAnnotations;

namespace DinoWallet.Api.DTOs.Requests;

public abstract class WalletRequestBase
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(128)]
    public string IdempotencyKey { get; set; } = default!;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

public class TopUpRequest : WalletRequestBase
{
    /// <summary>Treasury account to debit. If not provided, system selects the
    /// matching Treasury for the user account's asset type.</summary>
    public Guid? TreasuryAccountId { get; set; }
}

public class BonusRequest : WalletRequestBase
{
    public Guid? TreasuryAccountId { get; set; }
}

public class SpendRequest : WalletRequestBase
{
    public Guid? TreasuryAccountId { get; set; }
}
