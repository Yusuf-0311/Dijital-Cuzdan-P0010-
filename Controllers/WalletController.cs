using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Extensions;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _wallets;

    public WalletController(IWalletService wallets) => _wallets = wallets;

    /// <summary>Cüzdan bilgisi ve bakiye.</summary>
    [HttpGet]
    public async Task<IActionResult> GetWallet(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _wallets.GetWalletAsync(userId.Value, ct));
    }

    /// <summary>Para yatır.</summary>
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] MoneyRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(
            await _wallets.DepositAsync(userId.Value, request.Amount, request.Description, ct));
    }

    /// <summary>Para çek.</summary>
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] MoneyRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(
            await _wallets.WithdrawAsync(userId.Value, request.Amount, request.Description, ct));
    }

    /// <summary>
    /// İşlem geçmişi (yeniden eskiye, sayfalı).
    /// Örnek: /api/wallet/transactions?type=Payment&amp;from=2026-11-01&amp;to=2026-11-30&amp;page=1&amp;pageSize=20
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilter filter, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _wallets.GetTransactionsAsync(userId.Value, filter, ct));
    }

    /// <summary>Dekont: referans numarasıyla tek işlemin detayı.</summary>
    [HttpGet("transactions/{referenceNo}")]
    public async Task<IActionResult> GetReceipt(string referenceNo, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _wallets.GetReceiptAsync(userId.Value, referenceNo, ct));
    }
}
