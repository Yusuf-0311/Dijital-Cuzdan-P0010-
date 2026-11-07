namespace SecureWallet.Api.Models;

/// <summary>
/// Kullanıcının kaydettiği, sık kullanılan ödeme (örn. "Ev elektriği").
/// Her ödemede UsageCount artar; liste en çok kullanılandan sıralanır.
/// </summary>
public class SavedPayment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BillerId { get; set; }
    public string SubscriberNo { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;

    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Biller? Biller { get; set; }
}
