using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Services;

public interface ITransferService
{
    /// <summary>Giriş yapmış kullanıcının cüzdanından başka bir cüzdana para gönderir.</summary>
    Task<ServiceResult<TransactionDto>> TransferAsync(
        int senderUserId, int receiverWalletId, decimal amount, string? description, CancellationToken ct = default);
}
