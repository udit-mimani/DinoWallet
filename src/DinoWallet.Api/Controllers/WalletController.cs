using DinoWallet.Api.DTOs.Requests;
using DinoWallet.Api.DTOs.Responses;
using DinoWallet.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DinoWallet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>
    /// Top-up: credit a user wallet (simulates real-money purchase of credits).
    /// Debits the system Treasury and credits the user account.
    /// </summary>
    /// <remarks>
    /// Provide a unique IdempotencyKey per request. Replaying the same key
    /// returns the original result without re-processing.
    ///
    /// Example:
    ///     POST /api/wallet/topup
    ///     {
    ///       "accountId": "...",
    ///       "amount": 100,
    ///       "idempotencyKey": "purchase-2024-001",
    ///       "description": "Purchased 100 Gold Coins"
    ///     }
    /// </remarks>
    [HttpPost("topup")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _walletService.TopUpAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Bonus: issue free credits to a user (referral bonus, daily reward, etc.).
    /// Debits the system Treasury and credits the user account.
    /// </summary>
    [HttpPost("bonus")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Bonus([FromBody] BonusRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _walletService.BonusAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Spend: debit a user wallet (purchase in-app item, unlock level, etc.).
    /// Debits the user account and credits the system Treasury.
    /// Returns 422 if the account has insufficient funds.
    /// </summary>
    [HttpPost("spend")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Spend([FromBody] SpendRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _walletService.SpendAsync(request, ct);
        return Ok(result);
    }
}
