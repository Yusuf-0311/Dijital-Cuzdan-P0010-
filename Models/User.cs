namespace SecureWallet.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // İlişki: Bir kullanıcının bir cüzdanı olur
    public Wallet? Wallet { get; set; }
}
