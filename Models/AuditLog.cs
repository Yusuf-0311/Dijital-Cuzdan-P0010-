namespace SecureWallet.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty; // Örn: "Giriş Yapıldı", "Para Gönderildi"
    public string IpAddress { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty; // JSON formatında işlem detayları
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
