using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SecureWallet.Api.Extensions;

public static class AuthRateLimitPolicies
{
    /// <summary>Giriş, kayıt ve şifre değiştirme uç noktaları.</summary>
    public const string Auth = "auth";

    internal const int PermitLimit = 10;
    internal static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
}

public static class AuthRateLimitExtensions
{
    /// <summary>
    /// Kimlik uç noktalarına IP başına dakikada 10 istek sınırı. Aşılırsa 429 döner.
    /// Hesap kilidi TEK bir hesabı korur; bu sınır ise bir saldırganın aynı IP'den
    /// BİRÇOK farklı hesabı sırayla denemesini (credential stuffing) yavaşlatır.
    /// Yusuf'un AddMoneyRateLimiting ile kurduğu rate limiter'a ikinci bir politika ekler;
    /// onun dosyasına dokunmadan, aynı RateLimiterOptions üzerine yapılandırma yapılır.
    /// </summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.Configure<RateLimiterOptions>(options =>
            options.AddPolicy(AuthRateLimitPolicies.Auth, CreateAuthPartition));
        return services;
    }

    // Giriş yapılmamış isteklerde kullanıcı Id'si olmadığı için anahtar IP adresidir.
    // Not: Uygulama ileride bir reverse proxy (ör. Docker + nginx) arkasına alınırsa
    // RemoteIpAddress proxy'nin adresi olur; o zaman ForwardedHeaders ayarı gerekir.
    internal static RateLimitPartition<string> CreateAuthPartition(HttpContext httpContext) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AuthRateLimitPolicies.PermitLimit,
                Window = AuthRateLimitPolicies.Window,
                QueueLimit = 0
            });
}
