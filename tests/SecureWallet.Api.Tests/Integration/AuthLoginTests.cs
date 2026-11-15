using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using SecureWallet.Api.Controllers;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class AuthLoginTests
{
    private readonly PostgresFixture _fx;

    public AuthLoginTests(PostgresFixture fx) => _fx = fx;

    private async Task<(AuthUserDto User, string Password)> RegisterAsync()
    {
        var name = "k" + Guid.NewGuid().ToString("N")[..10];
        const string password = "Sifre1234";

        await using var db = _fx.CreateDbContext();
        var result = await AuthTestData.CreateAuthService(db).RegisterAsync(
            new RegisterRequest { Username = name, Email = $"{name}@test.local", Password = password });

        return (result.Data!, password);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokenForThatUser()
    {
        var (user, password) = await RegisterAsync();

        await using var db = _fx.CreateDbContext();
        var result = await AuthTestData.CreateAuthService(db).LoginAsync(
            new LoginRequest { Email = user.Email.ToUpperInvariant(), Password = password }); // e-posta büyük harfle

        Assert.True(result.Success, result.Message);
        Assert.Equal(user.Id, result.Data!.UserId);
        Assert.Equal(user.Id.ToString(), new JsonWebToken(result.Data.AccessToken).Subject);
    }

    [Fact]
    public async Task Login_WrongPassword_And_UnknownEmail_GiveTheSameAnswer()
    {
        var (user, _) = await RegisterAsync();

        await using var db = _fx.CreateDbContext();
        var service = AuthTestData.CreateAuthService(db);

        var wrongPassword = await service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Yanlis1234" });
        var unknownEmail = await service.LoginAsync(new LoginRequest { Email = "yok@test.local", Password = "Sifre1234" });

        Assert.Equal(ErrorCode.InvalidCredentials, wrongPassword.Error);
        Assert.Equal(ErrorCode.InvalidCredentials, unknownEmail.Error);
        Assert.Equal(wrongPassword.Message, unknownEmail.Message); // hangisinin yanlış olduğu belli olmaz
    }

    [Fact]
    public async Task Login_Failure_IsWrittenToAuditLog()
    {
        var (user, _) = await RegisterAsync();

        await using (var db = _fx.CreateDbContext())
            await AuthTestData.CreateAuthService(db).LoginAsync(new LoginRequest { Email = user.Email, Password = "Yanlis1234" });

        await using var check = _fx.CreateDbContext();
        Assert.True(await check.AuditLogs.AnyAsync(a => a.UserId == user.Id && a.Action == "Başarısız Giriş"));
    }

    [Fact]
    public async Task LoginEndpoint_Returns200WithToken_Or401()
    {
        var (user, password) = await RegisterAsync();

        await using var db = _fx.CreateDbContext();
        var controller = new AuthController(AuthTestData.CreateAuthService(db)).WithUser(null);

        var ok = await controller.Login(new LoginRequest { Email = user.Email, Password = password }, CancellationToken.None);
        Assert.False(string.IsNullOrEmpty(ControllerTestHelper.OkValue<LoginResponse>(ok).AccessToken));

        var denied = await controller.Login(new LoginRequest { Email = user.Email, Password = "Yanlis1234" }, CancellationToken.None);
        ControllerTestHelper.Problem(denied, 401);
    }
}