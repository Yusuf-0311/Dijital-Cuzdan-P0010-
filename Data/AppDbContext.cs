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
    public DbSet<Biller> Billers { get; set; }
    public DbSet<SavedPayment> SavedPayments { get; set; }

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
            e.Property(t => t.SubscriberNo).HasMaxLength(30);

            e.HasIndex(t => t.ReferenceNo).IsUnique();
            e.HasIndex(t => new { t.SenderWalletId, t.CreatedAt });
            e.HasIndex(t => new { t.ReceiverWalletId, t.CreatedAt });

            // Finansal kayıt silinmemeli: cüzdan/kurum silinse bile işlem geçmişi korunur.
            e.HasOne(t => t.SenderWallet)
                .WithMany(w => w.SentTransactions)
                .HasForeignKey(t => t.SenderWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.ReceiverWallet)
                .WithMany(w => w.ReceivedTransactions)
                .HasForeignKey(t => t.ReceiverWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Biller)
                .WithMany()
                .HasForeignKey(t => t.BillerId)
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

                // Fatura ödemesinde kurum ve abone no zorunlu; diğer işlemlerde kurum olmaz.
                t.HasCheckConstraint("CK_Transactions_Payment_Biller", $"""
                    ("Type" = '{nameof(TransactionType.Payment)}'
                        AND "BillerId" IS NOT NULL AND "SubscriberNo" IS NOT NULL)
                    OR ("Type" <> '{nameof(TransactionType.Payment)}' AND "BillerId" IS NULL)
                    """);
            });
        });

        modelBuilder.Entity<Biller>(e =>
        {
            e.Property(b => b.Name).HasMaxLength(100).IsRequired();
            e.Property(b => b.Category).HasConversion<string>().HasMaxLength(20);
            e.Property(b => b.SubscriberNoLabel).HasMaxLength(30).IsRequired();
            e.HasIndex(b => b.Name).IsUnique();

            // Örnek kurumlar (kurgusal isimler). Migration ile veritabanına eklenir.
            e.HasData(
                new Biller { Id = 1, Name = "Kent Elektrik Dağıtım", Category = BillerCategory.Electricity, SubscriberNoLabel = "Tesisat No" },
                new Biller { Id = 2, Name = "Şehir Su İdaresi", Category = BillerCategory.Water, SubscriberNoLabel = "Abone No" },
                new Biller { Id = 3, Name = "Anadolu Doğalgaz", Category = BillerCategory.NaturalGas, SubscriberNoLabel = "Abone No" },
                new Biller { Id = 4, Name = "HızlıNet İnternet", Category = BillerCategory.Internet, SubscriberNoLabel = "Hizmet No" },
                new Biller { Id = 5, Name = "Mavi Mobil", Category = BillerCategory.MobilePhone, SubscriberNoLabel = "Telefon No" },
                new Biller { Id = 6, Name = "Üniversite Harç Ödemeleri", Category = BillerCategory.Education, SubscriberNoLabel = "Öğrenci No" });
        });

        modelBuilder.Entity<SavedPayment>(e =>
        {
            e.Property(s => s.SubscriberNo).HasMaxLength(30).IsRequired();
            e.Property(s => s.Nickname).HasMaxLength(50).IsRequired();

            // Aynı kullanıcı aynı kurum + abone numarasını iki kez kaydedemez.
            e.HasIndex(s => new { s.UserId, s.BillerId, s.SubscriberNo }).IsUnique();

            e.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade); // kayıtlı ödeme kişisel tercih, finansal kayıt değil

            e.HasOne(s => s.Biller)
                .WithMany()
                .HasForeignKey(s => s.BillerId)
                .OnDelete(DeleteBehavior.Restrict);
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
