using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthUserDto>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
}