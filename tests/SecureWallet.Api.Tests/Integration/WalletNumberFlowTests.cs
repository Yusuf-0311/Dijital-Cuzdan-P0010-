using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

/// <summary>Cüzdan numarasının uçtan uca kullanımı: görüntüleme, alıcı arama ve transfer.</summary>
[Collection(PostgresCollection.Name)]
public class WalletNumberFlowTests
{
    private readonly PostgresFixture _fx;

    public WalletNumberFlowTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task GetWallet_ReturnsWalletNumber()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var wallet = (await TestData.CreateWalletService(db).GetWalletAsync(user.UserId)).Data!;

        Assert.Equal(WalletNumber.Format(user.WalletId), wallet.WalletNumber);
    }

    [Fact]
    public async Task Transfer_ByWalletNumber_Succeeds()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 500m);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx);
        var number = WalletNumber.Format(receiver.WalletId).Replace(" ", "").ToLowerInvariant(); // "sw0000xxxx"

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db)
            .TransferAsync(sender.UserId, new TransferRequest(number, null, 120m, "Numara ile"));

        Assert.True(result.Success, result.Message);
        Assert.Equal(120m, await TestData.GetBalanceAsync(_fx, receiver.WalletId));
    }

    [Fact]
    public async Task FindRecipient_ByWalletNumber_ReturnsMaskedNameAndNumber()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx);
        var number = WalletNumber.Format(receiver.WalletId);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).FindRecipientAsync(sender.UserId, number);

        Assert.True(result.Success, result.Message);
        Assert.Equal(receiver.WalletId, result.Data!.WalletId);
        Assert.Equal(number, result.Data.WalletNumber);
        Assert.Equal(receiver.Username[..2] + "***", result.Data.MaskedName);
    }

    [Fact]
    public async Task Transfer_WithMistypedWalletNumber_IsNotSentToAnotherWallet()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 500m);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx);
        var correct = WalletNumber.Format(receiver.WalletId).Replace(" ", "");
        var lastDigit = correct[^1] - '0';
        var mistyped = correct[..^1] + (char)('0' + (lastDigit + 1) % 10); // kontrol hanesi bozuk

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db)
            .TransferAsync(sender.UserId, new TransferRequest(mistyped, null, 120m, null));

        Assert.Equal(ErrorCode.NotFound, result.Error);
        Assert.Equal(500m, await TestData.GetBalanceAsync(_fx, sender.WalletId));
    }

    [Fact]
    public async Task Register_RejectsUsernameThatLooksLikeWalletNumber()
    {
        await using var db = _fx.CreateDbContext();
        var result = await AuthTestData.CreateAuthService(db).RegisterAsync(new RegisterRequest
        {
            Username = "sw00000422",
            Email = $"{Guid.NewGuid():N}@test.local",
            Password = "Sifre1234"
        });

        Assert.Equal(ErrorCode.Validation, result.Error);
    }
}
