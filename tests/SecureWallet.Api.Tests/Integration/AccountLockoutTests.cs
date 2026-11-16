using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class AccountLockoutTests
{
    private const string Password = "Sifre1234";
    private readonly PostgresFixture _fx;

    public AccountLockoutTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task FiveWrongPasswords_LockAccount_AndEvenCorrectPasswordIsRejected()
    {
        var email = await RegisterAsync();

        for (var i = 1; i < AuthService.MaxFailedAttempts; i++)
            Assert.Equal(ErrorCode.InvalidCredentials, (await LoginAsync(email, "YanlisSifre1")).Error);

        var fifth = await LoginAsync(email, "YanlisSifre1");
        Assert.Equal(ErrorCode.AccountLocked, fifth.Error);

        // Kilit süresince doğru şifre de kabul edilmez.
        var correct = await LoginAsync(email, Password);
        Assert.Equal(ErrorCode.AccountLocked, correct.Error);
        Assert.Null(correct.Data);

        await using var db = _fx.CreateDbContext();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.NotNull(user.LockedUntil);
        Assert.True(user.LockedUntil > DateTime.UtcNow.AddMinutes(14));
        Assert.Equal(0, user.FailedLoginCount); // kilitlenince sayaç sıfırlanır
    }

    [Fact]
    public async Task AfterLockExpires_CorrectPasswordSucceeds_AndLockIsCleared()
    {
        var email = await RegisterAsync();

        // 15 dakika beklemek yerine kilidin süresinin dolduğu durumu veritabanında kuruyoruz.
        await using (var db = _fx.CreateDbContext())
        {
            await db.Users.Where(u => u.Email == email)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LockedUntil, DateTime.UtcNow.AddMinutes(-1)));
        }

        var result = await LoginAsync(email, Password);
        Assert.True(result.Success, result.Message);

        await using var read = _fx.CreateDbContext();
        var user = await read.Users.SingleAsync(u => u.Email == email);
        Assert.Null(user.LockedUntil);
    }

    [Fact]
    public async Task SuccessfulLogin_ResetsFailedAttemptCounter()
    {
        var email = await RegisterAsync();

        for (var i = 0; i < AuthService.MaxFailedAttempts - 1; i++)
            await LoginAsync(email, "YanlisSifre1");

        Assert.True((await LoginAsync(email, Password)).Success);

        // Sayaç sıfırlandığı için 4 yeni hatalı deneme hesabı kilitlememeli.
        for (var i = 0; i < AuthService.MaxFailedAttempts - 1; i++)
            Assert.Equal(ErrorCode.InvalidCredentials, (await LoginAsync(email, "YanlisSifre1")).Error);
    }

    [Fact]
    public async Task ParallelWrongPasswords_AreAllCounted_AndAccountGetsLocked()
    {
        var email = await RegisterAsync();

        // Aynı anda 5 hatalı istek: sayaç atomik artmasaydı bazı denemeler kaybolur, hesap kilitlenmezdi.
        var attempts = Enumerable.Range(0, AuthService.MaxFailedAttempts)
            .Select(_ => LoginAsync(email, "YanlisSifre1"));
        var results = await Task.WhenAll(attempts);

        Assert.Contains(results, r => r.Error == ErrorCode.AccountLocked);

        await using var db = _fx.CreateDbContext();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.NotNull(user.LockedUntil);
    }

    [Fact]
    public async Task UnknownEmail_RepeatedAttempts_NeverReportLocked()
    {
        // Kayıtlı olmayan e-postada kilit mesajı dönmemeli; dönseydi e-postanın kayıtlı olduğu sanılırdı.
        var email = $"yok_{Guid.NewGuid():N}@ornek.com";

        for (var i = 0; i < AuthService.MaxFailedAttempts + 1; i++)
            Assert.Equal(ErrorCode.InvalidCredentials, (await LoginAsync(email, "YanlisSifre1")).Error);
    }

    // Her deneme ayrı DbContext ile yapılır: gerçek uygulamada her HTTP isteğinin kendi context'i vardır.
    private async Task<ServiceResult<LoginResponse>> LoginAsync(string email, string password)
    {
        await using var db = _fx.CreateDbContext();
        return await AuthTestData.CreateAuthService(db)
            .LoginAsync(new LoginRequest { Email = email, Password = password });
    }

    private async Task<string> RegisterAsync()
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var email = $"kilit_{id}@ornek.com";

        await using var db = _fx.CreateDbContext();
        var result = await AuthTestData.CreateAuthService(db)
            .RegisterAsync(new RegisterRequest { Username = $"kilit_{id}", Email = email, Password = Password });
        Assert.True(result.Success, result.Message);

        return email;
    }
}