using DinoWallet.Api.DTOs.Responses;
using DinoWallet.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DinoWallet.Api.Controllers;

/// <summary>Query accounts and balances.</summary>
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public AccountsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>List all wallet accounts (users and system/treasury accounts).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var accounts = await _walletService.GetAccountsAsync(ct);
        return Ok(accounts);
    }

    /// <summary>Get the current computed balance for a specific account.</summary>
    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken ct)
    {
        var balance = await _walletService.GetBalanceAsync(id, ct);
        return Ok(balance);
    }

    /// <summary>
    /// Get paginated ledger history for a specific account.
    /// Returns entries ordered by most recent first.
    /// </summary>
    [HttpGet("{id:guid}/ledger")]
    [ProducesResponseType(typeof(LedgerPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLedger(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        if (skip < 0)
            return BadRequest(new ErrorResponse { Error = "skip must be >= 0." });
        if (take < 1 || take > 100)
            return BadRequest(new ErrorResponse { Error = "take must be between 1 and 100." });

        var ledger = await _walletService.GetLedgerAsync(id, skip, take, ct);
        ledger.Skip = skip;
        ledger.Take = take;
        return Ok(ledger);
    }
}
