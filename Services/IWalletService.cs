using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Services;

public interface IWalletService
{
    /// <summary>Kullanıcı için 0 bakiyeli cüzdan oluşturur. Zaten varsa mevcut cüzdanı döndürür (tekrar çağrılabilir).</summary>
    Task<ServiceResult<WalletDto>> CreateWalletAsync(int userId, string currency = "TRY", CancellationToken ct = default);

    Task<ServiceResult<WalletDto>> GetWalletAsync(int userId, CancellationToken ct = default);

    Task<ServiceResult<TransactionDto>> DepositAsync(int userId, decimal amount, string? description, CancellationToken ct = default);

    Task<ServiceResult<TransactionDto>> WithdrawAsync(int userId, decimal amount, string? description, CancellationToken ct = default);

    Task<ServiceResult<PagedResult<TransactionDto>>> GetTransactionsAsync(int userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Dekont: yalnızca işlemin tarafı olan kullanıcı görebilir.</summary>
    Task<ServiceResult<TransactionDto>> GetReceiptAsync(int userId, string referenceNo, CancellationToken ct = default);
}
