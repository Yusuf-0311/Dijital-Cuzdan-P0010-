using Microsoft.AspNetCore.Http;
using SecureWallet.Api.Data;
using SecureWallet.Api.Models;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Infrastructure;

public sealed record TestUser(int UserId, int WalletId, string Username, string Email);

/// <summary>Testler için kullanıcı/cüzdan oluşturma ve servis kurma yardımcıları.</summary>
public static class TestData
{
    /// <summary>Benzersiz isimli kullanıcı + istenen bakiyeli cüzdan oluşturur (testler birbirini etkilemez).</summary>
    public static async Task<TestUser> CreateUserWithWalletAsync(
        PostgresFixture fx, decimal balance = 0m, string currency = "TRY")
    {
        var user = await CreateUserAsync(fx);

        await using var db = fx.CreateDbContext();
        var wallet = new Wallet { UserId = user.Id, Balance = balance, Currency = currency };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        return new TestUser(user.Id, wallet.Id, user.Username, user.Email);
    }

    public static async Task<User> CreateUserAsync(PostgresFixture fx)
    {
        await using var db = fx.CreateDbContext();
        var name = "u" + Guid.NewGuid().ToString("N")[..12];
        var user = new User { Username = name, Email = $"{name}@test.local", PasswordHash = "test-hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<decimal> GetBalanceAsync(PostgresFixture fx, int walletId)
    {
        await using var db = fx.CreateDbContext();
        return (await db.Wallets.FindAsync(walletId))!.Balance;
    }

    private static AuditService Audit(AppDbContext db) => new(db, new HttpContextAccessor());

    public static WalletService CreateWalletService(AppDbContext db) => new(db, Audit(db));
    public static TransferService CreateTransferService(AppDbContext db) => new(db, Audit(db));
    public static PaymentService CreatePaymentService(AppDbContext db) => new(db, Audit(db));
}
