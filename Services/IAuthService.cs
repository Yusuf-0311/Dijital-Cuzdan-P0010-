using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Services;

public interface IAuthService
{
    /// <summary>Kullanıcıyı kaydeder ve aynı işlem içinde 0 bakiyeli cüzdanını oluşturur.</summary>
    Task<ServiceResult<AuthUserDto>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}