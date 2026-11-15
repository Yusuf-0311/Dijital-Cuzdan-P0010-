using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

/// <summary>JWT ayarları. Key appsettings'te DEĞİL, user-secrets'ta tutulur.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Required, MinLength(32)] public string Key { get; set; } = string.Empty;
    [Range(1, 1440)] public int ExpiryMinutes { get; set; } = 15;
}

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public AccessToken CreateAccessToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,       // bileti kim verdi
            Audience = _options.Audience,   // bilet kimin için
            Expires = expiresAt,
            Subject = new ClaimsIdentity(new[]
            {
                // "sub" = kullanıcı Id'si. ASP.NET bunu okurken ClaimTypes.NameIdentifier'a çevirir;
                // Yusuf'un GetUserId() metodu ikisini de okuyor.
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // her biletin benzersiz numarası
                // E-posta ve şifre bilgisi BİLİNÇLİ OLARAK yok: token'ın içi herkes tarafından okunabilir.
            }),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}