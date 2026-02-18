using DinoWallet.Api.Data;
using DinoWallet.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace DinoWallet.IntegrationTests.Fixtures;

/// <summary>
/// Spins up a real PostgreSQL container via Testcontainers for integration tests.
/// One container is shared across all tests in the collection; each test resets its own data.
/// </summary>
public class WalletApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("dinowallet_test")
        .WithUsername("wallet_test")
        .WithPassword("wallet_test_pass")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    // IAsyncLifetime — called once before all tests in the collection
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    // IAsyncLifetime — called once after all tests in the collection
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.StopAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real DB registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WalletDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Register pointing at the test container
            services.AddDbContext<WalletDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });

        builder.UseEnvironment("Test");
    }

    /// <summary>
    /// Recreate the schema and re-seed for test isolation.
    /// Call at the start of each test (or test class) that needs a clean DB.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        // Do NOT auto-seed here — tests create their own minimal data
    }

    /// <summary>Get a scoped DbContext for direct data assertions in tests.</summary>
    public WalletDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    }
}

[CollectionDefinition("WalletTests")]
public class WalletTestCollection : ICollectionFixture<WalletApiFactory> { }
