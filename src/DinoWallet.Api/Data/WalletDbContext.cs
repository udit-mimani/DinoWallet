using DinoWallet.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DinoWallet.Api.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── AssetType ────────────────────────────────────────────────────────
        modelBuilder.Entity<AssetType>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Symbol).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.Symbol).IsUnique();
        });

        // ── Account ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            e.HasOne(x => x.AssetType)
             .WithMany(a => a.Accounts)
             .HasForeignKey(x => x.AssetTypeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Transaction ───────────────────────────────────────────────────────
        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityAlwaysColumn();
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            // Unique constraint — DB-level guarantee of exactly-once processing
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        // ── LedgerEntry ───────────────────────────────────────────────────────
        modelBuilder.Entity<LedgerEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityAlwaysColumn();
            e.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            e.HasOne(x => x.Account)
             .WithMany(a => a.LedgerEntries)
             .HasForeignKey(x => x.AccountId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Transaction)
             .WithMany(t => t.Entries)
             .HasForeignKey(x => x.TransactionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── IdempotencyRecord ─────────────────────────────────────────────────
        modelBuilder.Entity<IdempotencyRecord>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(128);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        });
    }
}
