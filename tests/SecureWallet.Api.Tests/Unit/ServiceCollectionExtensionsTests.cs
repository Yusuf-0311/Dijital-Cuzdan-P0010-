using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureWallet.Api.Data;
using SecureWallet.Api.Extensions;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Unit;

/// <summary>Program.cs'teki servis kayıtlarının eksiksiz ve çözülebilir olduğunu doğrular.</summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AllApplicationServices_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Bağlantı açılmaz; sadece DI grafiğinin kurulabildiği kontrol edilir.
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql("Host=localhost;Database=test;Username=test;Password=test"));
        services.AddWalletServices();
        services.AddGlobalErrorHandling();
        services.AddMoneyRateLimiting();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.IsType<WalletService>(scope.ServiceProvider.GetRequiredService<IWalletService>());
        Assert.IsType<TransferService>(scope.ServiceProvider.GetRequiredService<ITransferService>());
        Assert.IsType<PaymentService>(scope.ServiceProvider.GetRequiredService<IPaymentService>());
        Assert.IsType<AuditService>(scope.ServiceProvider.GetRequiredService<IAuditService>());

        var rateLimiter = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        Assert.Equal(429, rateLimiter.RejectionStatusCode);
    }
}
