using System;
using System.Text.Json.Serialization;

namespace SecureWallet.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    
    public int WalletId { get; set; }
    
    [JsonIgnore]
    public Wallet? Wallet { get; set; }
    
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
