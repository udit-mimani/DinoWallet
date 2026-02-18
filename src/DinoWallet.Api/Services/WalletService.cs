using System.Data;
using DinoWallet.Api.Data;
using DinoWallet.Api.Domain;
using DinoWallet.Api.DTOs.Requests;
using DinoWallet.Api.DTOs.Responses;
using DinoWallet.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DinoWallet.Api.Services;

public class WalletService : IWalletService
{
    private readonly WalletDbContext _db;
    private readonly ILogger<WalletService> _logger;

    public WalletService(WalletDbContext db, ILogger<WalletService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public Task<TransactionResponse> TopUpAsync(TopUpRequest request, CancellationToken ct = default)
        => ExecuteTransferAsync(
            userAccountId: request.AccountId,
            treasuryOverride: request.TreasuryAccountId,
            amount: request.Amount,
            type: TransactionType.TopUp,
            description: request.Description,
            idempotencyKey: request.IdempotencyKey,
            isCreditToUser: true,
            ct: ct);

    public Task<TransactionResponse> BonusAsync(BonusRequest request, CancellationToken ct = default)
        => ExecuteTransferAsync(
            userAccountId: request.AccountId,
            treasuryOverride: request.TreasuryAccountId,
            amount: request.Amount,
            type: TransactionType.Bonus,
            description: request.Description,
            idempotencyKey: request.IdempotencyKey,
            isCreditToUser: true,
            ct: ct);

    public Task<TransactionResponse> SpendAsync(SpendRequest request, CancellationToken ct = default)
        => ExecuteTransferAsync(
            userAccountId: request.AccountId,
            treasuryOverride: request.TreasuryAccountId,
            amount: request.Amount,
            type: TransactionType.Spend,
            description: request.Description,
            idempotencyKey: request.IdempotencyKey,
            isCreditToUser: false,
            ct: ct);

    public async Task<BalanceResponse> GetBalanceAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .Include(a => a.AssetType)
            .FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new AccountNotFoundException(accountId);

        var balance = await _db.LedgerEntries
            .Where(e => e.AccountId == accountId)
            .SumAsync(e => e.Amount, ct);

        return new BalanceResponse
        {
            AccountId = account.Id,
            AccountName = account.Name,
            AssetType = account.AssetType.Name,
            AssetSymbol = account.AssetType.Symbol,
            Balance = balance
        };
    }

    public async Task<LedgerPageResponse> GetLedgerAsync(
        Guid accountId, int skip, int take, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new AccountNotFoundException(accountId);

        var total = await _db.LedgerEntries
            .CountAsync(e => e.AccountId == accountId, ct);

        var runningBalance = await _db.LedgerEntries
            .Where(e => e.AccountId == accountId)
            .SumAsync(e => e.Amount, ct);

        var entries = await _db.LedgerEntries
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(Math.Clamp(take, 1, 100))
            .Select(e => new LedgerEntryResponse
            {
                Id = e.Id,
                AccountId = e.AccountId,
                AccountName = account.Name,
                Amount = e.Amount,
                TransactionId = e.TransactionId,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);

        return new LedgerPageResponse
        {
            AccountId = accountId,
            AccountName = account.Name,
            RunningBalance = runningBalance,
            TotalEntries = total,
            Entries = entries
        };
    }

    public async Task<List<AccountResponse>> GetAccountsAsync(CancellationToken ct = default)
    {
        return await _db.Accounts
            .Include(a => a.AssetType)
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Name)
            .Select(a => new AccountResponse
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type.ToString(),
                AssetType = a.AssetType.Name,
                AssetSymbol = a.AssetType.Symbol,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);
    }

    // ── Core Transfer Logic ────────────────────────────────────────────────────

    /// <summary>
    /// Single execution path for all three flows (TopUp, Bonus, Spend).
    ///
    /// Concurrency + Deadlock-Free protocol:
    ///   1.  Fast idempotency pre-check  (no lock — fast path for 99% of duplicates)
    ///   2.  Resolve accounts
    ///   3.  BEGIN TRANSACTION (ReadCommitted)
    ///   4.  Acquire pg_advisory_xact_lock for BOTH accounts in ascending UUID order
    ///       → deterministic lock ordering eliminates circular waits (deadlock prevention)
    ///   5.  Re-check idempotency inside transaction (TOCTOU race guard)
    ///   6.  SELECT … FOR UPDATE on account rows  (row-level pessimistic lock)
    ///   7.  Validate balance for Spend — guaranteed to see committed state
    ///   8.  INSERT Transaction + 2× LedgerEntry  (double-entry bookkeeping)
    ///   9.  INSERT IdempotencyRecord with correct TransactionId
    ///   10. COMMIT
    /// </summary>
    private async Task<TransactionResponse> ExecuteTransferAsync(
        Guid userAccountId,
        Guid? treasuryOverride,
        decimal amount,
        TransactionType type,
        string description,
        string idempotencyKey,
        bool isCreditToUser,
        CancellationToken ct)
    {
        if (amount <= 0)
            throw new InvalidAmountException("Amount must be greater than zero.");

        // Step 1 — fast pre-check (no transaction overhead for duplicate requests)
        var preCheck = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == idempotencyKey, ct);
        if (preCheck != null)
        {
            _logger.LogInformation("Idempotent (pre-check) key={Key}", idempotencyKey);
            return await BuildResponseFromTransactionId(preCheck.TransactionId, ct);
        }

        // Step 2 — resolve accounts outside the transaction (read-only lookups)
        var userAccount = await _db.Accounts
            .Include(a => a.AssetType)
            .FirstOrDefaultAsync(a => a.Id == userAccountId, ct)
            ?? throw new AccountNotFoundException(userAccountId);

        Guid treasuryId;
        if (treasuryOverride.HasValue)
        {
            if (!await _db.Accounts.AnyAsync(a => a.Id == treasuryOverride.Value, ct))
                throw new AccountNotFoundException(treasuryOverride.Value);
            treasuryId = treasuryOverride.Value;
        }
        else
        {
            var treasury = await _db.Accounts.FirstOrDefaultAsync(a =>
                a.Type == AccountType.System &&
                a.AssetTypeId == userAccount.AssetTypeId, ct)
                ?? throw new InvalidOperationException(
                    $"No Treasury account found for asset '{userAccount.AssetType.Name}'. " +
                    "Pass TreasuryAccountId explicitly or create a System account for this asset type.");
            treasuryId = treasury.Id;
        }

        var debitId  = isCreditToUser ? treasuryId    : userAccountId;
        var creditId = isCreditToUser ? userAccountId : treasuryId;

        // Steps 3-10 — single atomic transaction
        await using var dbTxn = await _db.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, ct);
        try
        {
            // Step 4 — advisory locks in ascending UUID order prevents deadlocks.
            // hashtext() maps the UUID string to an int8 advisory lock slot.
            // pg_advisory_xact_lock auto-releases on COMMIT/ROLLBACK.
            foreach (var lockId in new[] { debitId, creditId }.Distinct().OrderBy(id => id))
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock(hashtext({0}))",
                    lockId,
                    ct);
            }

            // Step 5 — re-check idempotency after acquiring the lock.
            // Two identical requests can both pass the pre-check; only one can win the lock.
            // The winner inserts; the loser finds the record here and returns the cached result.
            var innerCheck = await _db.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.Key == idempotencyKey, ct);
            if (innerCheck != null)
            {
                _logger.LogInformation("Idempotent (inner-check) key={Key}", idempotencyKey);
                await dbTxn.RollbackAsync(ct);
                return await BuildResponseFromTransactionId(innerCheck.TransactionId, ct);
            }

            // Step 6 — row-level locks via SELECT … FOR UPDATE.
            // EF Core does not support .Include() after .FromSqlRaw(), so we lock
            // the rows here and load navigation properties separately below.
            var lockedDebit = await _db.Accounts
                .FromSqlRaw("""SELECT * FROM "Accounts" WHERE "Id" = {0} FOR UPDATE""", debitId)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct)
                ?? throw new AccountNotFoundException(debitId);

            var lockedCredit = await _db.Accounts
                .FromSqlRaw("""SELECT * FROM "Accounts" WHERE "Id" = {0} FOR UPDATE""", creditId)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct)
                ?? throw new AccountNotFoundException(creditId);

            // Step 7 — balance check (Spend only).
            // Runs after the FOR UPDATE lock so we see committed, non-stale balance.
            if (!isCreditToUser)
            {
                var balance = await _db.LedgerEntries
                    .Where(e => e.AccountId == debitId)
                    .SumAsync(e => e.Amount, ct);

                if (balance < amount)
                {
                    await dbTxn.RollbackAsync(ct);
                    throw new InsufficientFundsException(balance, amount);
                }
            }

            // Step 8 — double-entry: every event creates exactly two ledger lines.
            // Debit line: negative (money leaving source)
            // Credit line: positive (money entering destination)
            var now = DateTime.UtcNow;
            var transaction = new Transaction
            {
                Type = type,
                Description = string.IsNullOrWhiteSpace(description) ? type.ToString() : description,
                IdempotencyKey = idempotencyKey,
                CreatedAt = now,
                Entries =
                [
                    new LedgerEntry { AccountId = debitId,  Amount = -amount, CreatedAt = now },
                    new LedgerEntry { AccountId = creditId, Amount =  amount, CreatedAt = now }
                ]
            };
            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync(ct);
            // transaction.Id is now populated by PostgreSQL IDENTITY column

            // Step 9 — store idempotency record with the real TransactionId.
            _db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Key = idempotencyKey,
                TransactionId = transaction.Id,
                CreatedAt = now
            });
            await _db.SaveChangesAsync(ct);

            // Step 10 — commit
            await dbTxn.CommitAsync(ct);

            _logger.LogInformation(
                "Committed txn={Id} type={Type} key={Key} debit={Debit} credit={Credit} amount={Amount}",
                transaction.Id, type, idempotencyKey, debitId, creditId, amount);

            return MapToResponse(transaction, lockedDebit, lockedCredit);
        }
        catch (Exception ex) when (ex is not InsufficientFundsException
                                       and not AccountNotFoundException
                                       and not InvalidAmountException
                                       and not InvalidOperationException)
        {
            try { await dbTxn.RollbackAsync(CancellationToken.None); } catch { /* best-effort */ }
            _logger.LogError(ex, "Unexpected error, transaction rolled back. key={Key}", idempotencyKey);
            throw;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<TransactionResponse> BuildResponseFromTransactionId(
        long transactionId, CancellationToken ct)
    {
        var txn = await _db.Transactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct)
            ?? throw new InvalidOperationException(
                $"Transaction {transactionId} referenced by idempotency key not found.");

        var accountIds = txn.Entries.Select(e => e.AccountId).Distinct().ToList();
        var nameMap = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, ct);

        return new TransactionResponse
        {
            TransactionId = txn.Id,
            Type = txn.Type.ToString(),
            Description = txn.Description,
            IdempotencyKey = txn.IdempotencyKey,
            CreatedAt = txn.CreatedAt,
            Entries = txn.Entries.Select(e => new LedgerEntryResponse
            {
                Id = e.Id,
                AccountId = e.AccountId,
                AccountName = nameMap.GetValueOrDefault(e.AccountId, "Unknown"),
                Amount = e.Amount,
                TransactionId = e.TransactionId,
                CreatedAt = e.CreatedAt
            }).ToList()
        };
    }

    private static TransactionResponse MapToResponse(
        Transaction txn, Account debitAccount, Account creditAccount)
    {
        return new TransactionResponse
        {
            TransactionId = txn.Id,
            Type = txn.Type.ToString(),
            Description = txn.Description,
            IdempotencyKey = txn.IdempotencyKey,
            CreatedAt = txn.CreatedAt,
            Entries = txn.Entries.Select(e => new LedgerEntryResponse
            {
                Id = e.Id,
                AccountId = e.AccountId,
                AccountName = e.AccountId == debitAccount.Id ? debitAccount.Name : creditAccount.Name,
                Amount = e.Amount,
                TransactionId = e.TransactionId,
                CreatedAt = e.CreatedAt
            }).ToList()
        };
    }
}
