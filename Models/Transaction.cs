namespace SecureWallet.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int SenderWalletId { get; set; }
    public int ReceiverWalletId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
