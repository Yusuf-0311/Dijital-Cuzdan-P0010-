using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();

            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Username).IsUnique();

            // Bir kullanıcının bir cüzdanı var. Finansal kayıtlar tutulacağı için
            // kullanıcı silinince cüzdan otomatik silinmez (Restrict).
            e.HasOne(u => u.Wallet)
                .WithOne(w => w.User)
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Wallet>(e =>
        {
            e.Property(w => w.Balance).HasPrecision(18, 2).HasDefaultValue(0m);
            e.Property(w => w.Currency).HasMaxLength(3).IsRequired();

            // PostgreSQL xmin -> optimistic concurrency (eşzamanlı bakiye bozulmasını önler)
            e.Property(w => w.Version)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            e.ToTable(t => t.HasCheckConstraint("CK_Wallets_Balance_NonNegative", "\"Balance\" >= 0"));
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.Property(t => t.ReferenceNo).HasMaxLength(32).IsRequired();
            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.SenderBalanceAfter).HasPrecision(18, 2);
            e.Property(t => t.ReceiverBalanceAfter).HasPrecision(18, 2);
            e.Property(t => t.Currency).HasMaxLength(3).IsRequired();
            e.Property(t => t.Description).HasMaxLength(250);

            e.HasIndex(t => t.ReferenceNo).IsUnique();
            e.HasIndex(t => new { t.SenderWalletId, t.CreatedAt });
            e.HasIndex(t => new { t.ReceiverWalletId, t.CreatedAt });

            // Finansal kayıt silinmemeli: cüzdan silinse bile işlem geçmişi korunur.
            e.HasOne(t => t.SenderWallet)
                .WithMany(w => w.SentTransactions)
                .HasForeignKey(t => t.SenderWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.ReceiverWallet)
                .WithMany(w => w.ReceivedTransactions)
                .HasForeignKey(t => t.ReceiverWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Transactions_Amount_Positive", "\"Amount\" > 0");

                // İşlem tipine göre gönderen/alıcı kuralı
                t.HasCheckConstraint("CK_Transactions_Type_Wallets", $"""
                    ("Type" = '{nameof(TransactionType.Deposit)}'
                        AND "SenderWalletId" IS NULL AND "ReceiverWalletId" IS NOT NULL)
                    OR ("Type" IN ('{nameof(TransactionType.Withdraw)}', '{nameof(TransactionType.Payment)}')
                        AND "SenderWalletId" IS NOT NULL AND "ReceiverWalletId" IS NULL)
                    OR ("Type" = '{nameof(TransactionType.Transfer)}'
                        AND "SenderWalletId" IS NOT NULL AND "ReceiverWalletId" IS NOT NULL
                        AND "SenderWalletId" <> "ReceiverWalletId")
                    """);
            });
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.IpAddress).HasMaxLength(45); // IPv6 dahil
            e.Property(a => a.Details).HasColumnType("jsonb");

            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.Timestamp);
        });
    }
}
