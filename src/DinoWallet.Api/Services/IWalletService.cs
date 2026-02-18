using DinoWallet.Api.DTOs.Requests;
using DinoWallet.Api.DTOs.Responses;

namespace DinoWallet.Api.Services;

public interface IWalletService
{
    /// <summary>
    /// Credit a user's wallet (user purchased credits with real money).
    /// Debits the system Treasury, credits the user account.
    /// </summary>
    Task<TransactionResponse> TopUpAsync(TopUpRequest request, CancellationToken ct = default);

    /// <summary>
    /// Issue free credits to a user (referral bonus, reward, etc.).
    /// Debits the system Treasury, credits the user account.
    /// </summary>
    Task<TransactionResponse> BonusAsync(BonusRequest request, CancellationToken ct = default);

    /// <summary>
    /// Debit a user's wallet (user spends credits for in-app purchase).
    /// Debits the user account, credits the system Treasury.
    /// Throws InsufficientFundsException if balance is too low.
    /// </summary>
    Task<TransactionResponse> SpendAsync(SpendRequest request, CancellationToken ct = default);

    /// <summary>Returns the current computed balance for an account.</summary>
    Task<BalanceResponse> GetBalanceAsync(Guid accountId, CancellationToken ct = default);

    /// <summary>Returns paginated ledger history for an account.</summary>
    Task<LedgerPageResponse> GetLedgerAsync(Guid accountId, int skip, int take, CancellationToken ct = default);

    /// <summary>Lists all accounts.</summary>
    Task<List<AccountResponse>> GetAccountsAsync(CancellationToken ct = default);
}
