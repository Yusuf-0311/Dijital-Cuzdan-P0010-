using System;

namespace SecureWallet.Api.Models;

public class Wallet
{
    public int Id { get; set; }
    
    public int UserId { get; set; } // Kullanıcı ile ilişki
    
    // Bu veri CP odaklı mimarimizde şifrelenerek tutulacak ve transaction scope ile yönetilecek
    public decimal Balance { get; set; } 
    
    public string Currency { get; set; } = "TRY";
    
    // Hatayı çözen satır:
    public DateTime CreatedAt { get; set; } 
    
    // EF Core Navigation Property (Kullanıcı tablosuna referans)
    public User? User { get; set; }
}
