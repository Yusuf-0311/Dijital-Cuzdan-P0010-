namespace SecureWallet.Api.Models;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // Bakiye düz decimal olarak tutulur (şifrelenmez, DB'de toplama/kısıt çalışabilsin).
    // Korunma: erişim kontrolü, CHECK (Balance >= 0), transaction ve AuditLog.
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Eşzamanlılık belirteci. PostgreSQL'in xmin sistem sütununa eşlenir;
    /// aynı cüzdana iki işlem aynı anda yazarsa ikincisi DbUpdateConcurrencyException alır.
    /// </summary>
    public uint Version { get; set; }

    public User? User { get; set; }

    public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
}
