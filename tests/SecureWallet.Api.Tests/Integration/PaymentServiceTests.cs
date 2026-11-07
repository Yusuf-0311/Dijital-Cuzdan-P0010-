using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class PaymentServiceTests
{
    private const int ElectricityBillerId = 1; // migration ile eklenen örnek kurum
    private readonly PostgresFixture _fx;

    public PaymentServiceTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task GetBillers_ReturnsSeededBillers_AndFiltersByCategory()
    {
        await using var db = _fx.CreateDbContext();
        var service = TestData.CreatePaymentService(db);

        var all = await service.GetBillersAsync(null);
        var electricity = await service.GetBillersAsync("electricity");
        var invalid = await service.GetBillersAsync("Uzay");

        Assert.True(all.Data!.Count >= 6);
        Assert.All(electricity.Data!, b => Assert.Equal("Electricity", b.Category));
        Assert.Equal(ErrorCode.Validation, invalid.Error);
    }

    [Fact]
    public async Task PayBill_DeductsBalance_AndCanSaveForLater()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 300m);

        await using var db = _fx.CreateDbContext();
        var service = TestData.CreatePaymentService(db);
        var result = await service.PayBillAsync(user.UserId,
            new PayBillRequest(ElectricityBillerId, "1234567", 245.50m, "Kasım faturası", SaveForLater: true, Nickname: "Ev elektriği"));

        Assert.True(result.Success, result.Message);
        Assert.Equal("Payment", result.Data!.Type);
        Assert.Equal("1234567", result.Data.SubscriberNo);
        Assert.False(string.IsNullOrEmpty(result.Data.Counterparty)); // kurum adı
        Assert.Equal(54.50m, await TestData.GetBalanceAsync(_fx, user.WalletId));

        await using var read = _fx.CreateDbContext();
        var saved = await TestData.CreatePaymentService(read).GetSavedPaymentsAsync(user.UserId);
        var item = Assert.Single(saved.Data!);
        Assert.Equal("Ev elektriği", item.Nickname);
    }

    [Fact]
    public async Task PaySaved_IncrementsUsage_AndSortsMostUsedFirst()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 1000m);

        int rarelyUsedId, oftenUsedId;
        await using (var db = _fx.CreateDbContext())
        {
            var service = TestData.CreatePaymentService(db);
            rarelyUsedId = (await service.CreateSavedPaymentAsync(user.UserId, new CreateSavedPaymentRequest(2, "55555", "Su"))).Data!.Id;
            oftenUsedId = (await service.CreateSavedPaymentAsync(user.UserId, new CreateSavedPaymentRequest(4, "66666", "İnternet"))).Data!.Id;

            await service.PaySavedAsync(user.UserId, oftenUsedId, new PaySavedRequest(100m, null));
            await service.PaySavedAsync(user.UserId, oftenUsedId, new PaySavedRequest(100m, null));
        }

        await using var read = _fx.CreateDbContext();
        var list = (await TestData.CreatePaymentService(read).GetSavedPaymentsAsync(user.UserId)).Data!;

        Assert.Equal(oftenUsedId, list[0].Id);
        Assert.Equal(2, list[0].UsageCount);
        Assert.Equal(rarelyUsedId, list[1].Id);
        Assert.Equal(800m, await TestData.GetBalanceAsync(_fx, user.WalletId));
    }

    [Fact]
    public async Task CreateSavedPayment_Duplicate_ReturnsDuplicate()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);

        await using var db = _fx.CreateDbContext();
        var service = TestData.CreatePaymentService(db);
        var request = new CreateSavedPaymentRequest(ElectricityBillerId, "7777777", "Ev");

        Assert.True((await service.CreateSavedPaymentAsync(user.UserId, request)).Success);
        var again = await service.CreateSavedPaymentAsync(user.UserId, request);

        Assert.Equal(ErrorCode.Duplicate, again.Error);
    }

    [Fact]
    public async Task SavedPayments_OfAnotherUser_CannotBeUsedOrDeleted()
    {
        var owner = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);
        var attacker = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        await using var db = _fx.CreateDbContext();
        var service = TestData.CreatePaymentService(db);
        var savedId = (await service.CreateSavedPaymentAsync(owner.UserId, new CreateSavedPaymentRequest(3, "88888", "Doğalgaz"))).Data!.Id;

        var pay = await service.PaySavedAsync(attacker.UserId, savedId, new PaySavedRequest(10m, null));
        var delete = await service.DeleteSavedPaymentAsync(attacker.UserId, savedId);

        Assert.Equal(ErrorCode.NotFound, pay.Error);
        Assert.Equal(ErrorCode.NotFound, delete.Error);
        Assert.Equal(100m, await TestData.GetBalanceAsync(_fx, attacker.WalletId));

        await using var check = _fx.CreateDbContext();
        Assert.True(await check.SavedPayments.AnyAsync(s => s.Id == savedId));
    }

    [Fact]
    public async Task PayBill_WithInsufficientFunds_ChangesNothing()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 10m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreatePaymentService(db).PayBillAsync(user.UserId,
            new PayBillRequest(ElectricityBillerId, "1234567", 10.01m, null));

        Assert.Equal(ErrorCode.InsufficientFunds, result.Error);
        Assert.Equal(10m, await TestData.GetBalanceAsync(_fx, user.WalletId));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("ABC12345")]
    public async Task PayBill_WithInvalidSubscriberNo_ReturnsValidation(string subscriberNo)
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx, balance: 100m);

        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreatePaymentService(db).PayBillAsync(user.UserId,
            new PayBillRequest(ElectricityBillerId, subscriberNo, 10m, null));

        Assert.Equal(ErrorCode.Validation, result.Error);
    }
}
