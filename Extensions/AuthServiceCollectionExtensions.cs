using Microsoft.AspNetCore.Identity;
using SecureWallet.Api.Models;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Extensions;

public static class AuthServiceCollectionExtensions
{
    /// <summary>Kimlik doğrulama servislerini kaydeder.</summary>
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}