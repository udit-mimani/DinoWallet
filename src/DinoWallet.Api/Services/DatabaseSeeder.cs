using DinoWallet.Api.Data;
using DinoWallet.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DinoWallet.Api.Services;

/// <summary>
/// Seeds initial data on application startup (runs after EF migrations).
/// All operations are idempotent — safe to run multiple times.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(WalletDbContext db, ILogger logger)
    {
        // ── Asset Types ────────────────────────────────────────────────────────
        if (!await db.AssetTypes.AnyAsync())
        {
            db.AssetTypes.AddRange(
                new AssetType { Name = "Gold Coins",     Symbol = "GLD", IsActive = true },
                new AssetType { Name = "Diamonds",       Symbol = "DIA", IsActive = true },
                new AssetType { Name = "Loyalty Points", Symbol = "LPT", IsActive = true }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded asset types.");
        }

        var gld = await db.AssetTypes.FirstAsync(a => a.Symbol == "GLD");
        var dia = await db.AssetTypes.FirstAsync(a => a.Symbol == "DIA");
        var lpt = await db.AssetTypes.FirstAsync(a => a.Symbol == "LPT");

        // ── System (Treasury) Accounts ─────────────────────────────────────────
        if (!await db.Accounts.AnyAsync(a => a.Type == AccountType.System))
        {
            db.Accounts.AddRange(
                new Account { Name = "Treasury - Gold Coins",     Type = AccountType.System, AssetTypeId = gld.Id },
                new Account { Name = "Treasury - Diamonds",       Type = AccountType.System, AssetTypeId = dia.Id },
                new Account { Name = "Treasury - Loyalty Points", Type = AccountType.System, AssetTypeId = lpt.Id }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded treasury accounts.");
        }

        var treasury = await db.Accounts
            .FirstAsync(a => a.Type == AccountType.System && a.AssetTypeId == gld.Id);

        // ── User Accounts ──────────────────────────────────────────────────────
        if (!await db.Accounts.AnyAsync(a => a.Type == AccountType.User))
        {
            db.Accounts.AddRange(
                new Account { Name = "Alice", Type = AccountType.User, AssetTypeId = gld.Id },
                new Account { Name = "Bob",   Type = AccountType.User, AssetTypeId = gld.Id }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded user accounts.");
        }

        var alice = await db.Accounts.FirstAsync(a => a.Name == "Alice" && a.AssetTypeId == gld.Id);
        var bob   = await db.Accounts.FirstAsync(a => a.Name == "Bob"   && a.AssetTypeId == gld.Id);

        // ── Initial Balances via Double-Entry Ledger ───────────────────────────
        // Each balance grant = one Transaction + two LedgerEntries (debit treasury, credit user)

        await SeedInitialBalanceAsync(db, treasury, alice, 500m, "seed-alice-initial", logger);
        await SeedInitialBalanceAsync(db, treasury, bob,   300m, "seed-bob-initial",   logger);
    }

    private static async Task SeedInitialBalanceAsync(
        WalletDbContext db,
        Account treasury,
        Account user,
        decimal amount,
        string idempotencyKey,
        ILogger logger)
    {
        if (await db.IdempotencyRecords.AnyAsync(r => r.Key == idempotencyKey))
            return; // Already seeded

        var now = DateTime.UtcNow;
        var txn = new Transaction
        {
            Type = TransactionType.TopUp,
            Description = $"Initial seed balance for {user.Name}",
            IdempotencyKey = idempotencyKey,
            CreatedAt = now,
            Entries =
            [
                new LedgerEntry { AccountId = treasury.Id, Amount = -amount, CreatedAt = now },
                new LedgerEntry { AccountId = user.Id,     Amount =  amount, CreatedAt = now }
            ]
        };
        db.Transactions.Add(txn);
        await db.SaveChangesAsync();

        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = idempotencyKey,
            TransactionId = txn.Id,
            CreatedAt = now
        });
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded balance: {User} = {Amount} GLD", user.Name, amount);
    }
}
