using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Models;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

/// <summary>
/// Aynı cüzdana aynı anda gelen isteklerde bakiyenin bozulmadığını doğrular
/// (Wallet.Version = PostgreSQL xmin ile optimistic concurrency).
/// Her istek, gerçek uygulamadaki gibi kendi DbContext'ini kullanır.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ConcurrencyTests
{
    private readonly PostgresFixture _fx;

    public ConcurrencyTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task TwoParallelWithdrawals_ExceedingBalance_OnlyOneSucceeds()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        async Task<bool> WithdrawAsync()
        {
            await using var db = _fx.CreateDbContext();
            var result = await TestData.CreateWalletService(db).WithdrawAsync(user.UserId, 70m, null);
            return result.Success;
        }

        var results = await Task.WhenAll(WithdrawAsync(), WithdrawAsync());

        Assert.Equal(1, results.Count(ok => ok));
        Assert.Equal(30m, await TestData.GetBalanceAsync(_fx, user.WalletId));
    }

    [Fact]
    public async Task ManyParallelDeposits_BalanceMatchesRecordedTransactions()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);
        const int requests = 10;

        async Task<bool> DepositAsync()
        {
            await using var db = _fx.CreateDbContext();
            var result = await TestData.CreateWalletService(db).DepositAsync(user.UserId, 10m, null);
            return result.Success;
        }

        var results = await Task.WhenAll(Enumerable.Range(0, requests).Select(_ => DepositAsync()));
        var succeeded = results.Count(ok => ok);

        // Bazı istekler yeniden deneme sınırını aşıp 409 dönebilir; önemli olan
        // bakiyenin, kaydedilen işlemlerle birebir tutması (kayıp güncelleme olmaması).
        await using var check = _fx.CreateDbContext();
        var txCount = await check.Transactions.CountAsync(t => t.ReceiverWalletId == user.WalletId && t.Type == TransactionType.Deposit);

        Assert.True(succeeded >= 1);
        Assert.Equal(succeeded, txCount);
        Assert.Equal(succeeded * 10m, await TestData.GetBalanceAsync(_fx, user.WalletId));
    }

    [Fact]
    public async Task OpposingParallelTransfers_TotalMoneyIsPreserved()
    {
        var a = await TestData.CreateUserWithWalletAsync(_fx, balance: 500m);
        var b = await TestData.CreateUserWithWalletAsync(_fx, balance: 500m);

        async Task SendAsync(TestUser from, TestUser to)
        {
            await using var db = _fx.CreateDbContext();
            await TestData.CreateTransferService(db).TransferAsync(
                from.UserId, new TransferRequest(to.Email, null, 25m, null));
        }

        var tasks = Enumerable.Range(0, 6)
            .Select(i => i % 2 == 0 ? SendAsync(a, b) : SendAsync(b, a));
        await Task.WhenAll(tasks);

        var total = await TestData.GetBalanceAsync(_fx, a.WalletId) + await TestData.GetBalanceAsync(_fx, b.WalletId);
        Assert.Equal(1000m, total); // para yoktan var olmadı, kaybolmadı
    }
}
