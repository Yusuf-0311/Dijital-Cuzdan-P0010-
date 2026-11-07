using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Services;

public interface ITransferService
{
    /// <summary>
    /// Giriş yapmış kullanıcının cüzdanından başka bir cüzdana para gönderir.
    /// Alıcı; e-posta, kullanıcı adı veya cüzdan Id'si ile belirtilir.
    /// </summary>
    Task<ServiceResult<TransactionDto>> TransferAsync(int senderUserId, TransferRequest request, CancellationToken ct = default);

    /// <summary>Transfer öncesi alıcıyı bulur ve maskeli adını döndürür ("yu***").</summary>
    Task<ServiceResult<RecipientDto>> FindRecipientAsync(int senderUserId, string query, CancellationToken ct = default);

    /// <summary>Son para gönderilen kişiler (hızlı transfer için).</summary>
    Task<ServiceResult<IReadOnlyList<RecentRecipientDto>>> GetRecentRecipientsAsync(int userId, int take = 5, CancellationToken ct = default);
}
