using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureWallet.Api.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SecureWallet.Api.Tests.Unit;

public class JwtSetupTests
{
    private static IConfiguration Config(string? key) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "SecureWallet",
            ["Jwt:Audience"] = "SecureWallet.Client",
            ["Jwt:Key"] = key
        })
        .Build();

    [Fact]
    public async Task ValidConfiguration_RegistersBearerScheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJwtAuthentication(Config(new string('k', 48)));

        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.NotNull(await schemes.GetSchemeAsync("Bearer"));
    }

    [Theory]
    [InlineData(null)]          // anahtar hiç yok
    [InlineData("kisa-anahtar")] // 32 karakterden kısa
    public void MissingOrShortKey_StopsTheApplication(string? key)
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddJwtAuthentication(Config(key)));
    }

    [Fact]
    public void Swagger_HasBearerSecurityDefinition()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSwaggerWithJwt();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value;

        Assert.True(options.SwaggerGeneratorOptions.SecuritySchemes.ContainsKey("Bearer"));
    }
}