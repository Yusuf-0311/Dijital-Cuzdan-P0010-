using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SecureWallet.Api.Models;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Unit;

public class TokenServiceTests
{
    private static readonly JwtOptions Settings = new()
    {
        Issuer = "SecureWallet",
        Audience = "SecureWallet.Client",
        Key = new string('k', 48),
        ExpiryMinutes = 15
    };

    private static TokenService CreateService() => new(Options.Create(Settings));

    private static TokenValidationParameters ValidationWith(string key) => new()
    {
        ValidIssuer = Settings.Issuer,
        ValidAudience = Settings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateLifetime = true
    };

    [Fact]
    public async Task Token_IsValid_AndCarriesUserId()
    {
        var token = CreateService().CreateAccessToken(new User { Id = 42, Username = "kadir" });

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, ValidationWith(Settings.Key));

        Assert.True(result.IsValid);
        Assert.Equal("42", ((JsonWebToken)result.SecurityToken).Subject);
    }

    [Fact]
    public void Token_ExpiresAfterConfiguredMinutes()
    {
        var before = DateTime.UtcNow;
        var token = CreateService().CreateAccessToken(new User { Id = 1, Username = "kadir" });

        Assert.InRange(token.ExpiresAt, before.AddMinutes(14), before.AddMinutes(16));
    }

    [Fact]
    public async Task Token_SignedWithAnotherKey_IsRejected()
    {
        var token = CreateService().CreateAccessToken(new User { Id = 42, Username = "kadir" });

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, ValidationWith(new string('x', 48)));

        Assert.False(result.IsValid); // anahtarı bilmeyen sahte bilet üretemez / doğrulatamaz
    }

    [Fact]
    public void Token_DoesNotContainEmailOrPasswordHash()
    {
        var user = new User { Id = 7, Username = "kadir", Email = "kadir@ornek.com", PasswordHash = "GIZLI_HASH" };

        var jwt = new JsonWebToken(CreateService().CreateAccessToken(user).Token);

        Assert.DoesNotContain(jwt.Claims, c => c.Value.Contains("kadir@ornek.com") || c.Value.Contains("GIZLI_HASH"));
    }
}