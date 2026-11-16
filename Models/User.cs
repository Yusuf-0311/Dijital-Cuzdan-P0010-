namespace SecureWallet.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Hesap kilitleme: art arda hatalı giriş sayısı ve kilidin açılacağı an (UTC).
    // Başarılı girişte sayaç sıfırlanır; LockedUntil null ise hesap kilitli değildir.
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    
    // İlişki: Bir kullanıcının bir cüzdanı olur
    public Wallet? Wallet { get; set; }
}
