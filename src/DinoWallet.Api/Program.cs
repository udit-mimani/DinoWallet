using DinoWallet.Api.Data;
using DinoWallet.Api.Middleware;
using DinoWallet.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Dino Ventures – Wallet Service",
        Version = "v1",
        Description = """
            Internal wallet service for managing virtual credits (Gold Coins, Diamonds, Loyalty Points).

            Supports three core flows:
            - **Top-Up**: Credit a user wallet (user purchased credits)
            - **Bonus**: Issue free credits (referral bonus, daily reward)
            - **Spend**: Debit a user wallet (in-app purchase)

            All transaction endpoints are **idempotent**: replaying the same IdempotencyKey
            returns the original result without re-processing.
            Balances are computed from a double-entry ledger — there is no mutable balance column.
            """
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// PostgreSQL + EF Core
builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5)));

// Business logic
builder.Services.AddScoped<IWalletService, WalletService>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

// Show Swagger in every environment except Production
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wallet Service v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
    });
}

app.MapControllers();

// Health-check endpoint (useful for docker-compose healthcheck and load balancers)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
