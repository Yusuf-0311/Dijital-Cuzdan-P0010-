using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Controllers;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class WalletSummaryTests
{
    private readonly PostgresFixture _fx;

    public WalletSummaryTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task Summary_ReturnsBalance_LatestFiveTransactions_AndMonthTotals()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);
        var other = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        await using (var db = _fx.CreateDbContext())
        {
            var wallets = TestData.CreateWalletService(db);
            var transfers = TestData.CreateTransferService(db);

            await wallets.DepositAsync(user.UserId, 100m, "Yatırma 1");
            await wallets.DepositAsync(user.UserId, 50m, "Yatırma 2");
            await wallets.DepositAsync(user.UserId, 25m, "Yatırma 3");
            await wallets.WithdrawAsync(user.UserId, 10m, "Çekme");
            await transfers.TransferAsync(other.UserId, new TransferRequest(user.Email, null, 30m, "Gelen"));
            await transfers.TransferAsync(user.UserId, new TransferRequest(other.Email, null, 20m, "Giden"));
        }

        await using var read = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(read).GetSummaryAsync(user.UserId);

        Assert.True(result.Success, result.Message);
        var summary = result.Data!;

        Assert.Equal(175m, summary.Wallet.Balance);                 // 100 + 50 + 25 - 10 + 30 - 20
        Assert.Equal(205m, summary.MonthIncoming);                  // 3 yatırma + gelen transfer
        Assert.Equal(30m, summary.MonthOutgoing);                   // çekme + giden transfer
        Assert.Equal(5, summary.RecentTransactions.Count);          // 6 işlemden en yeni 5'i
        Assert.Equal("Giden", summary.RecentTransactions[0].Description);
        Assert.DoesNotContain(summary.RecentTransactions, t => t.Description == "Yatırma 1"); // en eskisi listede yok
        Assert.Equal(1, summary.MonthStart.Day);
    }

    [Fact]
    public async Task Summary_ForUserWithoutWallet_ReturnsNotFound()
    {
        var userWithoutWallet = await TestData.CreateUserAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db).GetSummaryAsync(userWithoutWallet.Id);

        Assert.Equal(ErrorCode.NotFound, result.Error);
    }

    [Fact]
    public async Task SummaryEndpoint_Returns200ForUser_And401WithoutUser()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 42m);

        await using var db = _fx.CreateDbContext();
        var service = TestData.CreateWalletService(db);

        var ok = await new WalletController(service).WithUser(user.UserId).GetSummary(CancellationToken.None);
        Assert.Equal(42m, ControllerTestHelper.OkValue<WalletSummaryDto>(ok).Wallet.Balance);

        var anonymous = await new WalletController(service).WithUser(null).GetSummary(CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(anonymous);
    }
}
