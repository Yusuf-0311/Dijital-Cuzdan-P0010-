using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureWallet.Api.Extensions;

namespace SecureWallet.Api.Tests.Unit;

/// <summary>Web sitesinin API'ye erişim izninin sadece listedeki adreslere verildiğini doğrular.</summary>
public class CorsExtensionsTests
{
    private static ServiceProvider Build(params string[] origins)
    {
        var settings = origins
            .Select((o, i) => new KeyValuePair<string, string?>($"{CorsExtensions.AllowedOriginsKey}:{i}", o));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebClientCors(configuration);
        return services.BuildServiceProvider();
    }

    private static bool IsAllowed(ServiceProvider provider, string requestOrigin)
    {
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(CorsPolicies.WebClient)!;
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Headers.Origin = requestOrigin;

        return provider.GetRequiredService<ICorsService>().EvaluatePolicy(context, policy).IsOriginAllowed;
    }

    [Fact]
    public void ConfiguredOrigin_IsAllowed_OthersAreNot()
    {
        using var provider = Build("http://localhost:5173");

        Assert.True(IsAllowed(provider, "http://localhost:5173"));
        Assert.False(IsAllowed(provider, "http://kotu-site.example"));
        Assert.False(IsAllowed(provider, "http://localhost:9999")); // aynı makine, farklı port = farklı adres
    }

    [Fact]
    public void NoConfiguredOrigins_AllowsNothing()
    {
        using var provider = Build();

        Assert.False(IsAllowed(provider, "http://localhost:5173"));
    }

    [Fact]
    public void Policy_AllowsOnlyNeededHeadersAndMethods()
    {
        using var provider = Build("http://localhost:5173");
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(CorsPolicies.WebClient)!;

        Assert.False(policy.AllowAnyOrigin);
        Assert.False(policy.AllowAnyHeader);
        Assert.False(policy.SupportsCredentials); // JWT çerezle değil başlıkla gidiyor
        Assert.Equal(new[] { "GET", "POST", "DELETE" }, policy.Methods);
        Assert.Contains("Authorization", policy.Headers);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("localhost:5173")]               // şema yok
    [InlineData("http://localhost:5173/app")]    // yol var
    [InlineData("ftp://localhost:5173")]         // http/https değil
    public void InvalidOrigin_FailsAtStartup(string origin)
    {
        Assert.Throws<InvalidOperationException>(() => Build(origin));
    }
}
