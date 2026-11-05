namespace SecureWallet.Api.Models;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    // Bu veri CP odaklı mimarimizde şifrelenerek tutulacak ve transaction scope ile yönetilecek
    public decimal Balance { get; set; } 
    public string Currency { get; set; } = "TRY";
    
    public User? User { get; set; }
}
