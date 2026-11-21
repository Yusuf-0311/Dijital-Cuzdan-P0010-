using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureWallet.Api.Data;
using SecureWallet.Api.Extensions;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Unit;

public class DailyLimitRulesTests
{
    [Theory]
    // UTC 22:30 = Türkiye 01:30 (ertesi gün) → gün, UTC 21:00'de başlamış
    [InlineData("2026-11-21T22:30:00Z", "2026-11-21T21:00:00Z")]
    // UTC 20:59 = Türkiye 23:59 → hâlâ bir önceki gün
    [InlineData("2026-11-21T20:59:00Z", "2026-11-20T21:00:00Z")]
    // Tam gece yarısı (Türkiye 00:00)
    [InlineData("2026-11-21T21:00:00Z", "2026-11-21T21:00:00Z")]
    public void DayStartUtc_UsesTurkeyMidnight(string nowUtc, string expectedUtc)
    {
        var now = DateTime.Parse(nowUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        var expected = DateTime.Parse(expectedUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

        var start = DailyLimit.DayStartUtc(now);

        Assert.Equal(expected, start);
        Assert.Equal(DateTimeKind.Utc, start.Kind); // Npgsql timestamptz için UTC olmalı
    }

    [Fact]
    public void Check_AllowsExactlyReachingTheLimit()
    {
        Assert.Null(DailyLimit.Check(usedToday: 900m, amount: 100m, limit: 1_000m, currency: "TRY"));
    }

    [Fact]
    public void Check_RejectsAndReportsRemainingInTurkishFormat()
    {
        var message = DailyLimit.Check(usedToday: 98_765.50m, amount: 5_000m, limit: 100_000m, currency: "TRY");

        Assert.NotNull(message);
        Assert.Contains("1.234,50 TRY", message);
    }

    [Fact]
    public void Check_RemainingNeverNegative()
    {
        var message = DailyLimit.Check(usedToday: 1_200m, amount: 1m, limit: 1_000m, currency: "TRY");

        Assert.Contains("0,00 TRY", message);
    }

    [Fact]
    public void Options_AreReadFromConfiguration()
    {
        var options = BuildOptions(("WalletLimits:DailyOutgoingLimit", "250000"));

        Assert.Equal(250_000m, options.Value.DailyOutgoingLimit);
    }

    [Fact]
    public void Options_DefaultTo100000_WhenNotConfigured()
    {
        var options = BuildOptions();

        Assert.Equal(100_000m, options.Value.DailyOutgoingLimit);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void Options_RejectNonPositiveLimit(string value)
    {
        var options = BuildOptions(("WalletLimits:DailyOutgoingLimit", value));

        Assert.Throws<OptionsValidationException>(() => options.Value);
    }

    private static IOptions<WalletLimitOptions> BuildOptions(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql("Host=localhost;Database=test;Username=test;Password=test"));
        services.AddWalletServices(configuration);

        return services.BuildServiceProvider().GetRequiredService<IOptions<WalletLimitOptions>>();
    }
}
