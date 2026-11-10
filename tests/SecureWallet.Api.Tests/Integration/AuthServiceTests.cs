using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Models;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class AuthServiceTests
{
    private readonly PostgresFixture _fx;

    public AuthServiceTests(PostgresFixture fx) => _fx = fx;

    private static AuthService CreateService(AppDbContext db) =>
        new(db, TestData.CreateWalletService(db), new PasswordHasher<User>(), new AuditService(db, new HttpContextAccessor()));

    private static RegisterRequest NewRequest(string password = "Sifre1234")
    {
        var name = "k" + Guid.NewGuid().ToString("N")[..10];
        return new RegisterRequest { Username = name, Email = $"{name}@test.local", Password = password };
    }

    [Fact]
    public async Task Register_CreatesUserAndZeroBalanceWallet()
    {
        var request = NewRequest();
        request.Email = request.Email.ToUpperInvariant(); // büyük harfle gönder

        await using var db = _fx.CreateDbContext();
        var result = await CreateService(db).RegisterAsync(request);

        Assert.True(result.Success, result.Message);
        Assert.Equal(request.Email.ToLowerInvariant(), result.Data!.Email); // küçük harfe çevrildi

        await using var check = _fx.CreateDbContext();
        var wallet = await check.Wallets.SingleAsync(w => w.UserId == result.Data.Id);
        Assert.Equal(result.Data.WalletId, wallet.Id);
        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var request = NewRequest(password: "GizliSifre99");

        await using var db = _fx.CreateDbContext();
        var result = await CreateService(db).RegisterAsync(request);

        await using var check = _fx.CreateDbContext();
        var user = await check.Users.SingleAsync(u => u.Id == result.Data!.Id);

        Assert.DoesNotContain("GizliSifre99", user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, "GizliSifre99"));
    }

    [Fact]
    public async Task Register_SamePasswordForTwoUsers_ProducesDifferentHashes()
    {
        await using var db = _fx.CreateDbContext();
        var service = CreateService(db);
        var a = await service.RegisterAsync(NewRequest("AyniSifre1"));
        var b = await service.RegisterAsync(NewRequest("AyniSifre1"));

        await using var check = _fx.CreateDbContext();
        var hashA = (await check.Users.SingleAsync(u => u.Id == a.Data!.Id)).PasswordHash;
        var hashB = (await check.Users.SingleAsync(u => u.Id == b.Data!.Id)).PasswordHash;

        Assert.NotEqual(hashA, hashB); // her kullanıcıya ayrı salt
    }

    [Fact]
    public async Task Register_DuplicateEmail_InDifferentCase_IsRejected()
    {
        var first = NewRequest();
        await using (var db = _fx.CreateDbContext())
            Assert.True((await CreateService(db).RegisterAsync(first)).Success);

        var second = NewRequest();
        second.Email = first.Email.ToUpperInvariant();

        await using var db2 = _fx.CreateDbContext();
        var result = await CreateService(db2).RegisterAsync(second);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.Duplicate, result.Error);
    }

    [Fact]
    public async Task Register_DuplicateUsername_InDifferentCase_IsRejected_AndCreatesNothing()
    {
        var first = NewRequest();
        await using (var db = _fx.CreateDbContext())
            Assert.True((await CreateService(db).RegisterAsync(first)).Success);

        var second = NewRequest();
        second.Username = first.Username.ToUpperInvariant();

        await using var db2 = _fx.CreateDbContext();
        var result = await CreateService(db2).RegisterAsync(second);

        Assert.Equal(ErrorCode.Duplicate, result.Error);

        await using var check = _fx.CreateDbContext();
        Assert.False(await check.Users.AnyAsync(u => u.Email == second.Email)); // yarım kayıt kalmadı
    }
}