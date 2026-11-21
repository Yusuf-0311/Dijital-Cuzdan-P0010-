using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Controllers;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

/// <summary>PDF dekont uç noktası: sadece işlemin tarafı indirebilir.</summary>
[Collection(PostgresCollection.Name)]
public class ReceiptPdfTests
{
    private readonly PostgresFixture _fx;
    private readonly ReceiptPdfGenerator _pdf = new();

    public ReceiptPdfTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task Owner_DownloadsPdf()
    {
        var user = await TestData.CreateUserWithWalletAsync(_fx);
        var referenceNo = await DepositAsync(user, 150m);

        await using var db = _fx.CreateDbContext();
        var controller = new WalletController(TestData.CreateWalletService(db)).WithUser(user.UserId);

        var file = Assert.IsType<FileContentResult>(
            await controller.GetReceiptPdf(referenceNo, _pdf, CancellationToken.None));

        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal($"dekont-{referenceNo}.pdf", file.FileDownloadName);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(file.FileContents, 0, 4));
    }

    [Fact]
    public async Task Stranger_Gets404()
    {
        var owner = await TestData.CreateUserWithWalletAsync(_fx);
        var stranger = await TestData.CreateUserWithWalletAsync(_fx);
        var referenceNo = await DepositAsync(owner, 150m);

        await using var db = _fx.CreateDbContext();
        var controller = new WalletController(TestData.CreateWalletService(db)).WithUser(stranger.UserId);

        ControllerTestHelper.Problem(await controller.GetReceiptPdf(referenceNo, _pdf, CancellationToken.None), 404);
    }

    [Fact]
    public async Task Anonymous_Gets401()
    {
        await using var db = _fx.CreateDbContext();
        var controller = new WalletController(TestData.CreateWalletService(db)).WithUser(null);

        Assert.IsType<UnauthorizedResult>(await controller.GetReceiptPdf("TRX", _pdf, CancellationToken.None));
    }

    private async Task<string> DepositAsync(TestUser user, decimal amount)
    {
        await using var db = _fx.CreateDbContext();
        var result = await TestData.CreateWalletService(db).DepositAsync(user.UserId, amount, "Dekont testi");
        Assert.True(result.Success, result.Message);
        return result.Data!.ReferenceNo;
    }
}
