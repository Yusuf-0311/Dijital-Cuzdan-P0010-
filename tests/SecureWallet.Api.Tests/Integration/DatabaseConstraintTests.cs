using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Models;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

/// <summary>
/// Servis katmanı atlansa bile veritabanının hatalı veriyi reddettiğini doğrular
/// (savunma derinliği: CHECK kısıtları ve unique index'ler).
/// </summary>
[Collection(PostgresCollection.Name)]
public class DatabaseConstraintTests
{
    private readonly PostgresFixture _fx;

    public DatabaseConstraintTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task NegativeBalance_IsRejectedByDatabase()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 10m);

        await using var db = _fx.CreateDbContext();
        var wallet = await db.Wallets.SingleAsync(w => w.Id == user.WalletId);
        wallet.Balance = -1m;

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task NonPositiveAmount_IsRejectedByDatabase()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        db.Transactions.Add(NewTransaction(TransactionType.Deposit, receiverWalletId: user.WalletId, amount: 0m));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task TransferWithoutReceiver_IsRejectedByDatabase()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        db.Transactions.Add(NewTransaction(TransactionType.Transfer, senderWalletId: user.WalletId));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task PaymentWithoutBiller_IsRejectedByDatabase()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        db.Transactions.Add(NewTransaction(TransactionType.Payment, senderWalletId: user.WalletId));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DuplicateEmail_IsRejectedByDatabase()
    {
        var existing = await TestData.CreateUserAsync(_fx);

        await using var db = _fx.CreateDbContext();
        db.Users.Add(new User { Username = "baska" + Guid.NewGuid().ToString("N")[..8], Email = existing.Email, PasswordHash = "x" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static Transaction NewTransaction(
        TransactionType type, int? senderWalletId = null, int? receiverWalletId = null, decimal amount = 10m) => new()
    {
        ReferenceNo = "TST" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
        Type = type,
        Status = TransactionStatus.Completed,
        SenderWalletId = senderWalletId,
        ReceiverWalletId = receiverWalletId,
        Amount = amount,
        Currency = "TRY"
    };
}
