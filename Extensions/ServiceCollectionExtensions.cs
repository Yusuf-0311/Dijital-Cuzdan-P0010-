using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using SecureWallet.Api.Middleware;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Extensions;

public static class RateLimitPolicies
{
    /// <summary>Para hareketi yapan uç noktalar (yatır, çek, transfer, öde).</summary>
    public const string Money = "money";
}

public static class ServiceCollectionExtensions
{
    /// <summary>Cüzdan, transfer, ödeme ve audit servislerini kaydeder.</summary>
    public static IServiceCollection AddWalletServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor(); // AuditService IP adresini buradan okur
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }

    /// <summary>Hatalarda stack trace sızdırmayan, standart ProblemDetails yanıtı.</summary>
    public static IServiceCollection AddGlobalErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    /// <summary>
    /// Para hareketlerine hız sınırı: kullanıcı (yoksa IP) başına dakikada 10 istek.
    /// Otomatik/kötüye kullanım amaçlı toplu isteklere karşı koruma. Aşılırsa 429 döner.
    /// </summary>
    public static IServiceCollection AddMoneyRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.Money, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.GetUserId()?.ToString()
                                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                  ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });
        return services;
    }
}
