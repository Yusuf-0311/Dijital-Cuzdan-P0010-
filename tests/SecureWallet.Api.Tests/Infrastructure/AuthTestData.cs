using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SecureWallet.Api.Data;
using SecureWallet.Api.Models;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Infrastructure;

public static class AuthTestData
{
    public static readonly JwtOptions Jwt = new()
    {
        Issuer = "SecureWallet",
        Audience = "SecureWallet.Client",
        Key = new string('k', 48),
        ExpiryMinutes = 15
    };

    public static AuthService CreateAuthService(AppDbContext db) =>
        new(db,
            TestData.CreateWalletService(db),
            new PasswordHasher<User>(),
            new TokenService(Options.Create(Jwt)),
            new AuditService(db, new HttpContextAccessor()));
}