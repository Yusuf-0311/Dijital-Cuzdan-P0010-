using SecureWallet.Api.Controllers;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Models;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

/// <summary>
/// Günlük çıkış limiti: para çekme ve giden transferler ortak limite sayılır;
/// yatırma, gelen transfer, fatura ödemesi ve önceki günlerin işlemleri sayılmaz.
/// Testlerde okunaklı olsun diye limit 1.000 TRY verilir.
/// </summary>
[Collection(PostgresCollection.Name)]
public class DailyLimitTests
{
    private const decimal Limit = 1_000m;
    private readonly PostgresFixture _fx;

    public DailyLimitTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task Withdrawals_UpToLimit_Succeed_ThenNextIsRejected()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 5_000m);

        Assert.True((await WithdrawAsync(user, 600m)).Success);
        Assert.True((await WithdrawAsync(user, 400m)).Success); // tam limite kadar izin var

        var over = await WithdrawAsync(user, 0.01m);

        Assert.Equal(ErrorCode.LimitExceeded, over.Error);
        Assert.Contains("0,00 TRY", over.Message);
        Assert.Equal(4_000m, await TestData.GetBalanceAsync(_fx, user.WalletId)); // reddedilen işlem bakiyeye dokunmaz
    }

    [Fact]
    public async Task WithdrawalsAndTransfers_ShareTheSameLimit()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 5_000m);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx);

        Assert.True((await WithdrawAsync(sender, 600m)).Success);

        var tooMuch = await TransferAsync(sender, receiver, 500m);
        Assert.Equal(ErrorCode.LimitExceeded, tooMuch.Error);
        Assert.Contains("400,00 TRY", tooMuch.Message);

        Assert.True((await TransferAsync(sender, receiver, 400m)).Success);
        Assert.Equal(400m, await TestData.GetBalanceAsync(_fx, receiver.WalletId));
    }

    [Fact]
    public async Task DepositsIncomingTransfersAndBillPayments_DoNotCount()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 5_000m);
        var friend = await TestData.CreateUserWithWalletAsync(_fx, balance: 5_000m);

        await using (var db = _fx.CreateDbContext())
            Assert.True((await TestData.CreateWalletService(db, Limit).DepositAsync(user.UserId, 3_000m, null)).Success);

        Assert.True((await TransferAsync(friend, user, 900m)).Success); // gelen transfer

        await using (var db = _fx.CreateDbContext())
        {
            db.Transactions.Add(new Transaction
            {
                ReferenceNo = ReferenceNumber.Generate(),
                Type = TransactionType.Payment,
                Status = TransactionStatus.Completed,
                SenderWalletId = user.WalletId,
                BillerId = 1,
                SubscriberNo = "1234567",
                Amount = 800m,
                Currency = "TRY"
            });
            await db.SaveChangesAsync();
        }

        Assert.True((await WithdrawAsync(user, 1_000m)).Success); // limitin tamamı hâlâ kullanılabilir
    }

    [Fact]
    public async Task TransactionsBeforeToday_DoNotCount()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 5_000m);

        await using (var db = _fx.CreateDbContext())
        {
            db.Transactions.Add(new Transaction
            {
                ReferenceNo = ReferenceNumber.Generate(),
                Type = TransactionType.Withdraw,
                Status = TransactionStatus.Completed,
                SenderWalletId = user.WalletId,
                Amount = Limit,
                Currency = "TRY",
                // Türkiye saatiyle dün 23:59
                CreatedAt = DailyLimit.DayStartUtc(DateTime.UtcNow).AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        Assert.True((await WithdrawAsync(user, Limit)).Success);
    }

    [Fact]
    public async Task ParallelWithdrawals_CannotExceedLimitTogether()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 10_000m);

        // Bakiye ikisine de yeter, ama ikisi birlikte limiti (1.000) aşar.
        var results = await Task.WhenAll(WithdrawAsync(user, 600m), WithdrawAsync(user, 600m));

        Assert.Equal(1, results.Count(r => r.Success));
        Assert.Equal(9_400m, await TestData.GetBalanceAsync(_fx, user.WalletId));
    }

    [Fact]
    public async Task GetLimits_ReturnsUsedAndRemaining()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 5_000m);
        Assert.True((await WithdrawAsync(user, 300m)).Success);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db, Limit).GetLimitsAsync(user.UserId);

        Assert.True(result.Success, result.Message);
        var limits = result.Data!;
        Assert.Equal(Limit, limits.DailyLimit);
        Assert.Equal(300m, limits.UsedToday);
        Assert.Equal(700m, limits.RemainingToday);
        Assert.Equal("TRY", limits.Currency);
        Assert.InRange(limits.ResetsAt, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
    }

    [Fact]
    public async Task GetLimits_ForUserWithoutWallet_ReturnsNotFound()
    {
        var user = await TestData.CreateUserAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db).GetLimitsAsync(user.Id);

        Assert.Equal(ErrorCode.NotFound, result.Error);
    }

    [Fact]
    public async Task Controller_GetLimits_Returns200_AndAnonymousGets401()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        await using var db = _fx.CreateDbContext();
        var service = TestData.CreateWalletService(db);

        var ok = ControllerTestHelper.OkValue<WalletLimitsDto>(
            await new WalletController(service).WithUser(user.UserId).GetLimits(CancellationToken.None));
        Assert.Equal(WalletLimitOptions.DefaultDailyOutgoingLimit, ok.RemainingToday);

        var anonymous = await new WalletController(service).WithUser(null).GetLimits(CancellationToken.None);
        Assert.IsType<Microsoft.AspNetCore.Mvc.UnauthorizedResult>(anonymous);
    }

    // Her istek, gerçek uygulamadaki gibi kendi DbContext'ini kullanır.
    private async Task<ServiceResult<TransactionDto>> WithdrawAsync(TestUser user, decimal amount)
    {
        await using var db = _fx.CreateDbContext();
        return await TestData.CreateWalletService(db, Limit).WithdrawAsync(user.UserId, amount, null);
    }

    private async Task<ServiceResult<TransactionDto>> TransferAsync(TestUser from, TestUser to, decimal amount)
    {
        await using var db = _fx.CreateDbContext();
        return await TestData.CreateTransferService(db, Limit)
            .TransferAsync(from.UserId, new TransferRequest(to.Email, null, amount, null));
    }
}
