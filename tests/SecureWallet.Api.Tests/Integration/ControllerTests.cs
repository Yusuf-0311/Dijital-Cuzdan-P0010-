using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Controllers;
using SecureWallet.Api.Data;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

/// <summary>
/// Controller katmanını gerçek servisler ve gerçek PostgreSQL ile test eder:
/// kullanıcı Id'sinin token'dan okunması, kimliksiz isteğin reddedilmesi ve
/// servis sonuçlarının doğru HTTP yanıtlarına dönüşmesi.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ControllerTests
{
    private readonly PostgresFixture _fx;

    public ControllerTests(PostgresFixture fx) => _fx = fx;

    private static WalletController Wallet(AppDbContext db, int? userId) =>
        new WalletController(TestData.CreateWalletService(db)).WithUser(userId);

    private static TransferController Transfer(AppDbContext db, int? userId) =>
        new TransferController(TestData.CreateTransferService(db)).WithUser(userId);

    private static PaymentController Payment(AppDbContext db, int? userId) =>
        new PaymentController(TestData.CreatePaymentService(db)).WithUser(userId);

    [Fact]
    public async Task RequestsWithoutUser_AreRejectedWith401()
    {
        await using var db = _fx.CreateDbContext();
        var ct = CancellationToken.None;

        var wallet = Wallet(db, userId: null);
        Assert.IsType<UnauthorizedResult>(await wallet.GetWallet(ct));
        Assert.IsType<UnauthorizedResult>(await wallet.Deposit(new MoneyRequest(10m, null), ct));
        Assert.IsType<UnauthorizedResult>(await wallet.Withdraw(new MoneyRequest(10m, null), ct));
        Assert.IsType<UnauthorizedResult>(await wallet.GetTransactions(new TransactionFilter(null, null, null), ct));
        Assert.IsType<UnauthorizedResult>(await wallet.GetReceipt("TRX", ct));

        var transfer = Transfer(db, userId: null);
        Assert.IsType<UnauthorizedResult>(await transfer.Transfer(new TransferRequest("x", null, 1m, null), ct));
        Assert.IsType<UnauthorizedResult>(await transfer.FindRecipient("x", ct));
        Assert.IsType<UnauthorizedResult>(await transfer.RecentRecipients(5, ct));

        var payment = Payment(db, userId: null);
        Assert.IsType<UnauthorizedResult>(await payment.GetSaved(ct));
        Assert.IsType<UnauthorizedResult>(await payment.CreateSaved(new CreateSavedPaymentRequest(1, "12345", "x"), ct));
        Assert.IsType<UnauthorizedResult>(await payment.DeleteSaved(1, ct));
        Assert.IsType<UnauthorizedResult>(await payment.Pay(new PayBillRequest(1, "12345", 1m, null), ct));
        Assert.IsType<UnauthorizedResult>(await payment.PaySaved(1, new PaySavedRequest(1m, null), ct));
    }

    [Fact]
    public async Task WalletEndpoints_FullFlow()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);
        await using var db = _fx.CreateDbContext();
        var controller = Wallet(db, user.UserId);
        var ct = CancellationToken.None;

        var deposit = ControllerTestHelper.OkValue<TransactionDto>(await controller.Deposit(new MoneyRequest(100m, "Maaş"), ct));
        Assert.Equal(100m, deposit.BalanceAfter);

        var withdraw = ControllerTestHelper.OkValue<TransactionDto>(await controller.Withdraw(new MoneyRequest(30m, null), ct));
        Assert.Equal(70m, withdraw.BalanceAfter);

        var wallet = ControllerTestHelper.OkValue<WalletDto>(await controller.GetWallet(ct));
        Assert.Equal(70m, wallet.Balance);

        var history = ControllerTestHelper.OkValue<PagedResult<TransactionDto>>(
            await controller.GetTransactions(new TransactionFilter(null, null, null), ct));
        Assert.Equal(2, history.TotalCount);

        var receipt = ControllerTestHelper.OkValue<TransactionDto>(await controller.GetReceipt(deposit.ReferenceNo, ct));
        Assert.Equal("Maaş", receipt.Description);
    }

    [Fact]
    public async Task WalletEndpoints_MapErrorsToHttpStatus()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 5m);
        await using var db = _fx.CreateDbContext();
        var controller = Wallet(db, user.UserId);
        var ct = CancellationToken.None;

        ControllerTestHelper.Problem(await controller.Withdraw(new MoneyRequest(10m, null), ct), 422);
        ControllerTestHelper.Problem(await controller.Deposit(new MoneyRequest(-1m, null), ct), 400);
        ControllerTestHelper.Problem(await controller.GetReceipt("TRXYOK", ct), 404);
    }

    [Fact]
    public async Task TransferEndpoints_FullFlow()
    {
        var sender = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);
        var receiver = await TestData.CreateUserWithWalletAsync(_fx);
        await using var db = _fx.CreateDbContext();
        var controller = Transfer(db, sender.UserId);
        var ct = CancellationToken.None;

        var recipient = ControllerTestHelper.OkValue<RecipientDto>(await controller.FindRecipient(receiver.Email, ct));
        Assert.Equal(receiver.WalletId, recipient.WalletId);

        var tx = ControllerTestHelper.OkValue<TransactionDto>(
            await controller.Transfer(new TransferRequest(receiver.Username, null, 40m, null), ct));
        Assert.Equal(60m, tx.BalanceAfter);

        var recent = ControllerTestHelper.OkValue<IReadOnlyList<RecentRecipientDto>>(await controller.RecentRecipients(5, ct));
        Assert.Equal(receiver.WalletId, Assert.Single(recent).WalletId);

        ControllerTestHelper.Problem(await controller.Transfer(new TransferRequest(sender.Email, null, 1m, null), ct), 400);
    }

    [Fact]
    public async Task PaymentEndpoints_FullFlow()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 500m);
        await using var db = _fx.CreateDbContext();
        var controller = Payment(db, user.UserId);
        var ct = CancellationToken.None;

        var billers = ControllerTestHelper.OkValue<IReadOnlyList<BillerDto>>(await controller.GetBillers(null, ct));
        var billerId = billers[0].Id;

        var saved = ControllerTestHelper.OkValue<SavedPaymentDto>(
            await controller.CreateSaved(new CreateSavedPaymentRequest(billerId, "123456", "Ev"), ct));

        var list = ControllerTestHelper.OkValue<IReadOnlyList<SavedPaymentDto>>(await controller.GetSaved(ct));
        Assert.Single(list);

        var paid = ControllerTestHelper.OkValue<TransactionDto>(
            await controller.PaySaved(saved.Id, new PaySavedRequest(100m, null), ct));
        Assert.Equal(400m, paid.BalanceAfter);

        var direct = ControllerTestHelper.OkValue<TransactionDto>(
            await controller.Pay(new PayBillRequest(billerId, "999999", 50m, null), ct));
        Assert.Equal(350m, direct.BalanceAfter);

        Assert.IsType<NoContentResult>(await controller.DeleteSaved(saved.Id, ct));
        ControllerTestHelper.Problem(await controller.DeleteSaved(saved.Id, ct), 404);
    }
}
