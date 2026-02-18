using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DinoWallet.Api.Data;
using DinoWallet.Api.Domain;
using DinoWallet.Api.DTOs.Requests;
using DinoWallet.Api.DTOs.Responses;
using DinoWallet.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DinoWallet.IntegrationTests;

[Collection("WalletTests")]
public class WalletConcurrencyTests : IAsyncLifetime
{
    private readonly WalletApiFactory _factory;
    private readonly HttpClient _client;

    private Guid _treasuryId;
    private Guid _aliceId;
    private Guid _bobId;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WalletConcurrencyTests(WalletApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await SeedTestAccountsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Happy Path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TopUp_ValidRequest_ReturnsTransactionWithTwoLedgerEntries()
    {
        var response = await _client.PostAsJsonAsync("/api/wallet/topup", new TopUpRequest
        {
            AccountId = _aliceId,
            TreasuryAccountId = _treasuryId,
            Amount = 100m,
            IdempotencyKey = $"test-{Guid.NewGuid()}",
            Description = "Test top-up"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOpts);
        body!.TransactionId.Should().BeGreaterThan(0);
        body.Type.Should().Be("TopUp");
        body.Entries.Should().HaveCount(2);
        body.Entries.Should().Contain(e => e.Amount == -100m); // debit treasury
        body.Entries.Should().Contain(e => e.Amount ==  100m); // credit Alice
    }

    [Fact]
    public async Task Bonus_ValidRequest_CreditsUserFromTreasury()
    {
        var response = await _client.PostAsJsonAsync("/api/wallet/bonus", new BonusRequest
        {
            AccountId = _aliceId,
            TreasuryAccountId = _treasuryId,
            Amount = 50m,
            IdempotencyKey = $"test-{Guid.NewGuid()}",
            Description = "Referral bonus"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOpts);
        body!.Type.Should().Be("Bonus");
    }

    [Fact]
    public async Task Spend_WithSufficientFunds_DeductsCorrectly()
    {
        await GiveBalance(_aliceId, 200m);

        var response = await _client.PostAsJsonAsync("/api/wallet/spend", new SpendRequest
        {
            AccountId = _aliceId,
            TreasuryAccountId = _treasuryId,
            Amount = 75m,
            IdempotencyKey = $"test-{Guid.NewGuid()}",
            Description = "Buy item"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var balance = await GetBalance(_aliceId);
        balance.Should().Be(125m);
    }

    [Fact]
    public async Task GetBalance_ReturnsCorrectBalance()
    {
        await GiveBalance(_aliceId, 300m);

        var res = await _client.GetAsync($"/api/accounts/{_aliceId}/balance");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<BalanceResponse>(JsonOpts);
        body!.Balance.Should().Be(300m);
        body.AccountName.Should().Be("Alice");
        body.AssetSymbol.Should().Be("GLD");
    }

    [Fact]
    public async Task GetLedger_ReturnsPaginatedEntries()
    {
        await GiveBalance(_aliceId, 100m);
        await GiveBalance(_aliceId, 50m);

        var res = await _client.GetAsync($"/api/accounts/{_aliceId}/ledger?skip=0&take=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<LedgerPageResponse>(JsonOpts);
        body!.TotalEntries.Should().BeGreaterOrEqualTo(2);
        body.RunningBalance.Should().Be(150m);
    }

    [Fact]
    public async Task GetAccounts_ReturnsBothUsersAndTreasury()
    {
        var res = await _client.GetAsync("/api/accounts");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var accounts = await res.Content.ReadFromJsonAsync<List<AccountResponse>>(JsonOpts);
        accounts.Should().HaveCountGreaterOrEqualTo(3); // 1 treasury + 2 users
    }

    // ── Error Cases ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Spend_InsufficientFunds_Returns422()
    {
        // Alice has 0 balance — spend should fail
        var response = await _client.PostAsJsonAsync("/api/wallet/spend", new SpendRequest
        {
            AccountId = _aliceId,
            TreasuryAccountId = _treasuryId,
            Amount = 1m,
            IdempotencyKey = $"test-{Guid.NewGuid()}",
            Description = "Should fail"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOpts);
        error!.Error.Should().Contain("Insufficient");
    }

    [Fact]
    public async Task TopUp_NonExistentAccount_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/wallet/topup", new TopUpRequest
        {
            AccountId = Guid.NewGuid(), // doesn't exist
            TreasuryAccountId = _treasuryId,
            Amount = 100m,
            IdempotencyKey = $"test-{Guid.NewGuid()}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TopUp_ZeroAmount_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/wallet/topup", new TopUpRequest
        {
            AccountId = _aliceId,
            TreasuryAccountId = _treasuryId,
            Amount = 0m,
            IdempotencyKey = $"test-{Guid.NewGuid()}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBalance_NonExistentAccount_Returns404()
    {
        var res = await _client.GetAsync($"/api/accounts/{Guid.NewGuid()}/balance");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Idempotency Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task IdempotentTopUp_SameKeySentTwice_OnlyOneTransactionCreated()
    {
        var key = $"idem-{Guid.NewGuid()}";

        var r1 = await _client.PostAsJsonAsync("/api/wallet/topup", new TopUpRequest
        {
            AccountId = _aliceId,
            TreasuryAccountId = _treasuryId,
            Amount = 100m,
            IdempotencyKey = key,
            Description = "First send"
        });

        var r2 = await _client.PostAsJsonAsync("/api/wallet/topup", new TopUpRequest
        {
            AccountId = _aliceId,
            TreasuryAccountId = _treasuryId,
            Amount = 100m,
            IdempotencyKey = key,
            Description = "Duplicate send"
        });

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);

        var t1 = await r1.Content.ReadFromJsonAsync<TransactionResponse>(JsonOpts);
        var t2 = await r2.Content.ReadFromJsonAsync<TransactionResponse>(JsonOpts);

        // Same transaction ID — only one was created
        t1!.TransactionId.Should().Be(t2!.TransactionId);

        // Balance should only be 100, not 200
        var balance = await GetBalance(_aliceId);
        balance.Should().Be(100m);
    }

    [Fact]
    public async Task IdempotentRequest_TenConcurrentDuplicates_OnlyOneTransactionCreated()
    {
        var key = $"concurrent-idem-{Guid.NewGuid()}";

        var tasks = Enumerable.Range(0, 10).Select(_ =>
            _client.PostAsJsonAsync("/api/wallet/topup", new TopUpRequest
            {
                AccountId = _aliceId,
                TreasuryAccountId = _treasuryId,
                Amount = 50m,
                IdempotencyKey = key,
                Description = "Concurrent duplicate"
            })).ToArray();

        var responses = await Task.WhenAll(tasks);

        // All return 200
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        // All return same transaction ID
        var txnIds = new HashSet<long>();
        foreach (var r in responses)
        {
            var body = await r.Content.ReadFromJsonAsync<TransactionResponse>(JsonOpts);
            txnIds.Add(body!.TransactionId);
        }
        txnIds.Should().HaveCount(1, "all duplicates must resolve to the same transaction");

        // Balance credited exactly once
        var balance = await GetBalance(_aliceId);
        balance.Should().Be(50m);
    }

    // ── Concurrency Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task ConcurrentTopUps_AllSucceed_FinalBalanceIsExact()
    {
        const int n = 20;
        const decimal each = 10m;

        var tasks = Enumerable.Range(0, n).Select(i =>
            _client.PostAsJsonAsync("/api/wallet/topup", new TopUpRequest
            {
                AccountId = _aliceId,
                TreasuryAccountId = _treasuryId,
                Amount = each,
                IdempotencyKey = $"concurrent-topup-{Guid.NewGuid()}",
                Description = $"Concurrent #{i}"
            })).ToArray();

        var responses = await Task.WhenAll(tasks);
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        var balance = await GetBalance(_aliceId);
        balance.Should().Be(n * each, "every top-up must be recorded exactly once");
    }

    [Fact]
    public async Task ConcurrentSpends_OnlySucceedIfFundsAvailable_NegativeBalanceImpossible()
    {
        // Fund Alice with exactly 50 GLD (enough for 5 × 10 = 50, not 10 × 10 = 100)
        await GiveBalance(_aliceId, 50m);

        const int n = 10;
        const decimal each = 10m;

        var tasks = Enumerable.Range(0, n).Select(i =>
            _client.PostAsJsonAsync("/api/wallet/spend", new SpendRequest
            {
                AccountId = _aliceId,
                TreasuryAccountId = _treasuryId,
                Amount = each,
                IdempotencyKey = $"concurrent-spend-{Guid.NewGuid()}",
                Description = $"Concurrent spend #{i}"
            })).ToArray();

        var responses = await Task.WhenAll(tasks);

        var successes = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var failures  = responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);

        successes.Should().Be(5,  "only 5 spends of 10 can fit in a 50 balance");
        failures.Should().Be(5,   "the remaining 5 must be rejected with 422");

        // CRITICAL: balance must be exactly 0, never negative
        var balance = await GetBalance(_aliceId);
        balance.Should().Be(0m, "balance must never go negative");
    }

    [Fact]
    public async Task ConcurrentSpends_BalanceNeverGoesNegative()
    {
        await GiveBalance(_aliceId, 100m);

        // Fire 50 concurrent spends of 10 — only 10 can succeed
        var tasks = Enumerable.Range(0, 50).Select(i =>
            _client.PostAsJsonAsync("/api/wallet/spend", new SpendRequest
            {
                AccountId = _aliceId,
                TreasuryAccountId = _treasuryId,
                Amount = 10m,
                IdempotencyKey = $"stress-spend-{Guid.NewGuid()}"
            })).ToArray();

        await Task.WhenAll(tasks);

        var balance = await GetBalance(_aliceId);
        balance.Should().BeGreaterOrEqualTo(0m, "balance must NEVER be negative");
    }

    [Fact]
    public async Task DeadlockAvoidance_SimultaneousOperationsBothAccounts_BothComplete()
    {
        await GiveBalance(_aliceId, 100m);
        await GiveBalance(_bobId,   100m);

        // Alice and Bob both spend simultaneously (both touch the same treasury)
        // Without ordered locking this is a classic deadlock scenario
        var aliceTask = _client.PostAsJsonAsync("/api/wallet/spend", new SpendRequest
        {
            AccountId = _aliceId,
            TreasuryAccountId = _treasuryId,
            Amount = 30m,
            IdempotencyKey = $"deadlock-alice-{Guid.NewGuid()}"
        });

        var bobTask = _client.PostAsJsonAsync("/api/wallet/spend", new SpendRequest
        {
            AccountId = _bobId,
            TreasuryAccountId = _treasuryId,
            Amount = 30m,
            IdempotencyKey = $"deadlock-bob-{Guid.NewGuid()}"
        });

        await Task.WhenAll(aliceTask, bobTask);
        var aliceRes = await aliceTask;
        var bobRes = await bobTask;

        aliceRes.StatusCode.Should().Be(HttpStatusCode.OK, "Alice's spend should succeed without deadlock");
        bobRes.StatusCode.Should().Be(HttpStatusCode.OK,   "Bob's spend should succeed without deadlock");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task SeedTestAccountsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        var asset = new AssetType { Name = "Gold Coins", Symbol = "GLD", IsActive = true };
        db.AssetTypes.Add(asset);
        await db.SaveChangesAsync();

        var treasury = new Account { Name = "Treasury", Type = AccountType.System, AssetTypeId = asset.Id };
        var alice    = new Account { Name = "Alice",    Type = AccountType.User,   AssetTypeId = asset.Id };
        var bob      = new Account { Name = "Bob",      Type = AccountType.User,   AssetTypeId = asset.Id };

        db.Accounts.AddRange(treasury, alice, bob);
        await db.SaveChangesAsync();

        _treasuryId = treasury.Id;
        _aliceId    = alice.Id;
        _bobId      = bob.Id;
    }

    private async Task GiveBalance(Guid accountId, decimal amount)
    {
        var res = await _client.PostAsJsonAsync("/api/wallet/topup", new TopUpRequest
        {
            AccountId = accountId,
            TreasuryAccountId = _treasuryId,
            Amount = amount,
            IdempotencyKey = $"setup-{Guid.NewGuid()}"
        });
        res.EnsureSuccessStatusCode();
    }

    private async Task<decimal> GetBalance(Guid accountId)
    {
        var res  = await _client.GetAsync($"/api/accounts/{accountId}/balance");
        var body = await res.Content.ReadFromJsonAsync<BalanceResponse>(JsonOpts);
        return body!.Balance;
    }
}
