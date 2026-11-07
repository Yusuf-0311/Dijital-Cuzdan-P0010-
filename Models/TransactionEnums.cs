namespace SecureWallet.Api.Models;

public enum TransactionType
{
    Deposit = 1,   // Para yatırma  (sadece alıcı cüzdan dolu)
    Withdraw = 2,  // Para çekme    (sadece gönderen cüzdan dolu)
    Transfer = 3,  // Cüzdanlar arası transfer (ikisi de dolu)
    Payment = 4    // Fatura / sık kullanılan ödemeler (sadece gönderen cüzdan dolu)
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}
