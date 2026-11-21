using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Services;

public interface IWalletService
{
    /// <summary>Kullanıcı için 0 bakiyeli cüzdan oluşturur. Zaten varsa mevcut cüzdanı döndürür (tekrar çağrılabilir).</summary>
    Task<ServiceResult<WalletDto>> CreateWalletAsync(int userId, string currency = "TRY", CancellationToken ct = default);

    Task<ServiceResult<WalletDto>> GetWalletAsync(int userId, CancellationToken ct = default);

    /// <summary>Ana sayfa özeti: bakiye, son 5 işlem, bu ayın gelen ve giden toplamı.</summary>
    Task<ServiceResult<WalletSummaryDto>> GetSummaryAsync(int userId, CancellationToken ct = default);

    /// <summary>Günlük çıkış limiti: limit, bugün kullanılan, kalan ve sıfırlanma zamanı (UTC).</summary>
    Task<ServiceResult<WalletLimitsDto>> GetLimitsAsync(int userId, CancellationToken ct = default);

    Task<ServiceResult<TransactionDto>> DepositAsync(int userId, decimal amount, string? description, CancellationToken ct = default);

    Task<ServiceResult<TransactionDto>> WithdrawAsync(int userId, decimal amount, string? description, CancellationToken ct = default);

    /// <summary>İşlem geçmişi: yeniden eskiye, sayfalı, tip ve tarih aralığına göre filtrelenebilir.</summary>
    Task<ServiceResult<PagedResult<TransactionDto>>> GetTransactionsAsync(int userId, TransactionFilter filter, CancellationToken ct = default);

    /// <summary>Dekont: yalnızca işlemin tarafı olan kullanıcı görebilir.</summary>
    Task<ServiceResult<TransactionDto>> GetReceiptAsync(int userId, string referenceNo, CancellationToken ct = default);
}
