using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Controllers;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class ChangePasswordTests
{
    private const string OldPassword = "EskiSifre1";
    private const string NewPassword = "YeniSifre2";
    private readonly PostgresFixture _fx;

    public ChangePasswordTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_NewPasswordWorks_OldDoesNot()
    {
        var (userId, email) = await RegisterAsync();

        var result = await ChangeAsync(userId, OldPassword, NewPassword);
        Assert.True(result.Success, result.Message);

        Assert.True((await LoginAsync(email, NewPassword)).Success);
        Assert.Equal(ErrorCode.InvalidCredentials, (await LoginAsync(email, OldPassword)).Error);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_FailsAndKeepsOldPassword()
    {
        var (userId, email) = await RegisterAsync();

        var result = await ChangeAsync(userId, "YanlisSifre9", NewPassword);

        Assert.Equal(ErrorCode.Validation, result.Error);
        Assert.True((await LoginAsync(email, OldPassword)).Success);
    }

    [Fact]
    public async Task ChangePassword_ToSamePassword_IsRejected()
    {
        var (userId, _) = await RegisterAsync();

        var result = await ChangeAsync(userId, OldPassword, OldPassword);

        Assert.Equal(ErrorCode.Validation, result.Error);
    }

    [Fact]
    public async Task ChangePassword_ForUnknownUser_ReturnsNotFound()
    {
        var result = await ChangeAsync(int.MaxValue, OldPassword, NewPassword);

        Assert.Equal(ErrorCode.NotFound, result.Error);
    }

    [Fact]
    public async Task Controller_Success_Returns204()
    {
        var (userId, _) = await RegisterAsync();

        await using var db = _fx.CreateDbContext();
        var controller = new AuthController(AuthTestData.CreateAuthService(db)).WithUser(userId);

        var response = await controller.ChangePassword(
            new ChangePasswordRequest { CurrentPassword = OldPassword, NewPassword = NewPassword }, CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public async Task Controller_WrongCurrentPassword_Returns400()
    {
        var (userId, _) = await RegisterAsync();

        await using var db = _fx.CreateDbContext();
        var controller = new AuthController(AuthTestData.CreateAuthService(db)).WithUser(userId);

        var response = await controller.ChangePassword(
            new ChangePasswordRequest { CurrentPassword = "YanlisSifre9", NewPassword = NewPassword }, CancellationToken.None);

        ControllerTestHelper.Problem(response, StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Controller_WithoutUser_Returns401()
    {
        await using var db = _fx.CreateDbContext();
        var controller = new AuthController(AuthTestData.CreateAuthService(db)).WithUser(null);

        var response = await controller.ChangePassword(
            new ChangePasswordRequest { CurrentPassword = OldPassword, NewPassword = NewPassword }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response);
    }

    private async Task<ServiceResult<bool>> ChangeAsync(int userId, string current, string next)
    {
        await using var db = _fx.CreateDbContext();
        return await AuthTestData.CreateAuthService(db).ChangePasswordAsync(
            userId, new ChangePasswordRequest { CurrentPassword = current, NewPassword = next });
    }

    private async Task<ServiceResult<LoginResponse>> LoginAsync(string email, string password)
    {
        await using var db = _fx.CreateDbContext();
        return await AuthTestData.CreateAuthService(db)
            .LoginAsync(new LoginRequest { Email = email, Password = password });
    }

    private async Task<(int UserId, string Email)> RegisterAsync()
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var email = $"sifre_{id}@ornek.com";

        await using var db = _fx.CreateDbContext();
        var result = await AuthTestData.CreateAuthService(db)
            .RegisterAsync(new RegisterRequest { Username = $"sifre_{id}", Email = email, Password = OldPassword });
        Assert.True(result.Success, result.Message);

        return (result.Data!.Id, email);
    }
}