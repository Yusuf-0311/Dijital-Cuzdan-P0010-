namespace SecureWallet.Api.Models;

public class Transaction
{
    public int Id { get; set; }

    /// <summary>Dekontta gösterilen benzersiz referans numarası (servis üretir).</summary>
    public string ReferenceNo { get; set; } = string.Empty;

    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    // Yatırmada gönderen, çekme/ödemede alıcı olmaz; bu yüzden nullable.
    public int? SenderWalletId { get; set; }
    public int? ReceiverWalletId { get; set; }

    // Sadece Payment (fatura ödeme) işlemlerinde dolu.
    public int? BillerId { get; set; }
    public string? SubscriberNo { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }

    // İşlem anındaki bakiyeler: hareket geçmişi ve dekont için.
    public decimal? SenderBalanceAfter { get; set; }
    public decimal? ReceiverBalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Wallet? SenderWallet { get; set; }
    public Wallet? ReceiverWallet { get; set; }
    public Biller? Biller { get; set; }
}
