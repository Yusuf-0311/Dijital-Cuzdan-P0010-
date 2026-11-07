using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class TransferServiceTests
{
    private readonly PostgresFixture _fx;

    public TransferServiceTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task Transfer_ByEmail_MovesMoney_AndShowsCounterparty()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 200m);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx, balance: 10m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).TransferAsync(
            sender.UserId, new TransferRequest(receiver.Email.ToUpperInvariant(), null, 50m, "Kira payı"));

        Assert.True(result.Success, result.Message);
        Assert.Equal("Transfer", result.Data!.Type);
        Assert.Equal(receiver.Username, result.Data.Counterparty);
        Assert.Equal(150m, result.Data.BalanceAfter);
        Assert.Equal(150m, await TestData.GetBalanceAsync(_fx, sender.WalletId));
        Assert.Equal(60m, await TestData.GetBalanceAsync(_fx, receiver.WalletId));

        // Alıcı, işlemi kendi geçmişinde "gelen" olarak görür.
        await using var read = _fx.CreateDbContext();
        var history = await TestData.CreateWalletService(read).GetTransactionsAsync(receiver.UserId, new TransactionFilter("Transfer", null, null));
        var item = Assert.Single(history.Data!.Items);
        Assert.Equal("In", item.Direction);
        Assert.Equal(sender.Username, item.Counterparty);
        Assert.Equal(60m, item.BalanceAfter);
    }

    [Fact]
    public async Task Transfer_ByUsername_Works()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 20m);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).TransferAsync(
            sender.UserId, new TransferRequest(receiver.Username, null, 20m, null));

        Assert.True(result.Success, result.Message);
        Assert.Equal(0m, await TestData.GetBalanceAsync(_fx, sender.WalletId));
    }

    [Fact]
    public async Task Transfer_ToSelf_IsRejected()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).TransferAsync(
            user.UserId, new TransferRequest(user.Email, null, 10m, null));

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.SelfTransfer, result.Error);
    }

    [Fact]
    public async Task Transfer_WithInsufficientFunds_ChangesNothing()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 5m);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).TransferAsync(
            sender.UserId, new TransferRequest(null, receiver.WalletId, 6m, null));

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.InsufficientFunds, result.Error);
        Assert.Equal(5m, await TestData.GetBalanceAsync(_fx, sender.WalletId));
        Assert.Equal(0m, await TestData.GetBalanceAsync(_fx, receiver.WalletId));

        await using var check = _fx.CreateDbContext();
        Assert.False(await check.Transactions.AnyAsync(t => t.SenderWalletId == sender.WalletId));
    }

    [Fact]
    public async Task Transfer_BetweenDifferentCurrencies_IsRejected()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m, currency: "TRY");
        var receiver = await TestData.CreateUserWithWalletAsync(_fx, currency: "USD");

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).TransferAsync(
            sender.UserId, new TransferRequest(receiver.Email, null, 10m, null));

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.CurrencyMismatch, result.Error);
    }

    [Fact]
    public async Task Transfer_ToUnknownRecipient_ReturnsNotFound()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).TransferAsync(
            sender.UserId, new TransferRequest("yok-boyle-biri@test.local", null, 10m, null));

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.NotFound, result.Error);
    }

    [Fact]
    public async Task Transfer_WithoutRecipient_ReturnsValidation()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).TransferAsync(
            sender.UserId, new TransferRequest(null, null, 10m, null));

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.Validation, result.Error);
    }

    [Fact]
    public async Task FindRecipient_ReturnsMaskedName()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(db).FindRecipientAsync(sender.UserId, receiver.Email);

        Assert.True(result.Success);
        Assert.Equal(receiver.WalletId, result.Data!.WalletId);
        Assert.Equal(receiver.Username[..2] + "***", result.Data.MaskedName);
        Assert.DoesNotContain(receiver.Username, result.Data.MaskedName);
    }

    [Fact]
    public async Task RecentRecipients_AreOrderedByLastTransfer()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);
        var first = await TestData.CreateUserWithWalletAsync(_fx);
        var second = await TestData.CreateUserWithWalletAsync(_fx);

        await using (var db = _fx.CreateDbContext())
        {
            var service = TestData.CreateTransferService(db);
            await service.TransferAsync(sender.UserId, new TransferRequest(first.Email, null, 1m, null));
            await service.TransferAsync(sender.UserId, new TransferRequest(first.Email, null, 1m, null));
            await service.TransferAsync(sender.UserId, new TransferRequest(second.Email, null, 1m, null));
        }

        await using var read = _fx.CreateDbContext();
        var result = await TestData.CreateTransferService(read).GetRecentRecipientsAsync(sender.UserId);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal(second.WalletId, result.Data[0].WalletId); // en son gönderilen önce
        Assert.Equal(first.WalletId, result.Data[1].WalletId);
        Assert.Equal(2, result.Data[1].TransferCount);
    }
}
