using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Services;

public interface IPaymentService
{
    /// <summary>Ödeme yapılabilen kurumlar. category: Electricity, Water, NaturalGas, Internet, MobilePhone, Education.</summary>
    Task<ServiceResult<IReadOnlyList<BillerDto>>> GetBillersAsync(string? category, CancellationToken ct = default);

    /// <summary>Kayıtlı ödemeler, en sık kullanılandan başlayarak.</summary>
    Task<ServiceResult<IReadOnlyList<SavedPaymentDto>>> GetSavedPaymentsAsync(int userId, CancellationToken ct = default);

    Task<ServiceResult<SavedPaymentDto>> CreateSavedPaymentAsync(int userId, CreateSavedPaymentRequest request, CancellationToken ct = default);

    Task<ServiceResult<bool>> DeleteSavedPaymentAsync(int userId, int savedPaymentId, CancellationToken ct = default);

    /// <summary>Kurum + abone no ile fatura öder. İstenirse ödemeyi "sık kullanılanlar"a kaydeder.</summary>
    Task<ServiceResult<TransactionDto>> PayBillAsync(int userId, PayBillRequest request, CancellationToken ct = default);

    /// <summary>Kayıtlı ödeme üzerinden tek tıkla öder.</summary>
    Task<ServiceResult<TransactionDto>> PaySavedAsync(int userId, int savedPaymentId, PaySavedRequest request, CancellationToken ct = default);
}
