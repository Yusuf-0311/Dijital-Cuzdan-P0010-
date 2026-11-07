using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class WalletServiceTests
{
    private readonly PostgresFixture _fx;

    public WalletServiceTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task CreateWallet_IsIdempotent()
    {
        var user = await TestData.CreateUserAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var service = TestData.CreateWalletService(db);

        var first = await service.CreateWalletAsync(user.Id);
        var second = await service.CreateWalletAsync(user.Id);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Equal(0m, first.Data.Balance);
        Assert.Equal(1, await db.Wallets.CountAsync(w => w.UserId == user.Id));
    }

    [Fact]
    public async Task CreateWallet_ForUnknownUser_ReturnsNotFound()
    {
        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db).CreateWalletAsync(userId: -1);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.NotFound, result.Error);
    }

    [Fact]
    public async Task Deposit_IncreasesBalance_AndWritesTransactionAndAudit()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db).DepositAsync(user.UserId, 150.75m, "Maaş");

        Assert.True(result.Success);
        Assert.Equal("Deposit", result.Data!.Type);
        Assert.Equal("In", result.Data.Direction);
        Assert.Equal(150.75m, result.Data.BalanceAfter);
        Assert.Equal(150.75m, await TestData.GetBalanceAsync(_fx, user.WalletId));

        await using var check = _fx.CreateDbContext();
        Assert.True(await check.Transactions.AnyAsync(t => t.ReferenceNo == result.Data.ReferenceNo));
        Assert.True(await check.AuditLogs.AnyAsync(a => a.UserId == user.UserId && a.Action == "Para Yatırıldı"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("10.555")]
    public async Task Deposit_WithInvalidAmount_ReturnsValidation_AndChangesNothing(string amountText)
    {
        var amount = decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 50m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db).DepositAsync(user.UserId, amount, null);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.Validation, result.Error);
        Assert.Equal(50m, await TestData.GetBalanceAsync(_fx, user.WalletId));
    }

    [Fact]
    public async Task Withdraw_DecreasesBalance()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db).WithdrawAsync(user.UserId, 40m, null);

        Assert.True(result.Success);
        Assert.Equal("Out", result.Data!.Direction);
        Assert.Equal(60m, result.Data.BalanceAfter);
        Assert.Equal(60m, await TestData.GetBalanceAsync(_fx, user.WalletId));
    }

    [Fact]
    public async Task Withdraw_MoreThanBalance_ReturnsInsufficientFunds_AndChangesNothing()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 30m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db).WithdrawAsync(user.UserId, 30.01m, null);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.InsufficientFunds, result.Error);
        Assert.Equal(30m, await TestData.GetBalanceAsync(_fx, user.WalletId));

        await using var check = _fx.CreateDbContext();
        Assert.False(await check.Transactions.AnyAsync(t => t.SenderWalletId == user.WalletId));
    }

    [Fact]
    public async Task GetTransactions_FiltersByType_AndPaginatesNewestFirst()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);
        await using (var db = _fx.CreateDbContext())
        {
            var service = TestData.CreateWalletService(db);
            for (var i = 1; i <= 5; i++)
                await service.DepositAsync(user.UserId, i * 10m, $"Yatırma {i}");
            await service.WithdrawAsync(user.UserId, 5m, "Çekme");
        }

        await using var read = _fx.CreateDbContext();
        var service2 = TestData.CreateWalletService(read);

        var deposits = await service2.GetTransactionsAsync(user.UserId, new TransactionFilter("deposit", null, null, Page: 1, PageSize: 2));
        Assert.True(deposits.Success);
        Assert.Equal(5, deposits.Data!.TotalCount);
        Assert.Equal(2, deposits.Data.Items.Count);
        Assert.All(deposits.Data.Items, t => Assert.Equal("Deposit", t.Type));
        Assert.Equal("Yatırma 5", deposits.Data.Items[0].Description); // en yeni önce

        var all = await service2.GetTransactionsAsync(user.UserId, new TransactionFilter(null, null, null));
        Assert.Equal(6, all.Data!.TotalCount);

        var future = await service2.GetTransactionsAsync(user.UserId, new TransactionFilter(null, DateTime.UtcNow.AddDays(1), null));
        Assert.Equal(0, future.Data!.TotalCount);
    }

    [Fact]
    public async Task GetTransactions_WithUnknownType_ReturnsValidation()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);
        await using var db = _fx.CreateDbContext();

        var result = await TestData.CreateWalletService(db).GetTransactionsAsync(user.UserId, new TransactionFilter("Hediye", null, null));

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.Validation, result.Error);
    }

    [Fact]
    public async Task GetReceipt_IsVisibleOnlyToParties()
    {
        var owner = await TestData.CreateUserWithWalletAsync(_fx);
        var stranger = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var service = TestData.CreateWalletService(db);
        var deposit = await service.DepositAsync(owner.UserId, 20m, null);
        var refNo = deposit.Data!.ReferenceNo;

        var ownReceipt = await service.GetReceiptAsync(owner.UserId, refNo.ToLowerInvariant());
        var strangerReceipt = await service.GetReceiptAsync(stranger.UserId, refNo);

        Assert.True(ownReceipt.Success);
        Assert.Equal(20m, ownReceipt.Data!.Amount);
        Assert.False(strangerReceipt.Success);
        Assert.Equal(ErrorCode.NotFound, strangerReceipt.Error);
    }
}
