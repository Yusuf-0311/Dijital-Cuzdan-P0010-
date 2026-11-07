using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Extensions;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentController(IPaymentService payments) => _payments = payments;

    /// <summary>Ödeme yapılabilen kurumlar. İsteğe bağlı: ?category=Electricity</summary>
    [HttpGet("billers")]
    public async Task<IActionResult> GetBillers([FromQuery] string? category, CancellationToken ct)
        => this.ToActionResult(await _payments.GetBillersAsync(category, ct));

    /// <summary>Kayıtlı (sık kullanılan) ödemeler.</summary>
    [HttpGet("saved")]
    public async Task<IActionResult> GetSaved(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _payments.GetSavedPaymentsAsync(userId.Value, ct));
    }

    /// <summary>Kayıtlı ödeme ekle: { "billerId": 1, "subscriberNo": "1234567", "nickname": "Ev elektriği" }</summary>
    [HttpPost("saved")]
    public async Task<IActionResult> CreateSaved([FromBody] CreateSavedPaymentRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _payments.CreateSavedPaymentAsync(userId.Value, request, ct));
    }

    [HttpDelete("saved/{id:int}")]
    public async Task<IActionResult> DeleteSaved(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _payments.DeleteSavedPaymentAsync(userId.Value, id, ct);
        return result.Success ? NoContent() : this.ToActionResult(result);
    }

    /// <summary>
    /// Fatura öde: { "billerId": 1, "subscriberNo": "1234567", "amount": 245.50,
    ///               "saveForLater": true, "nickname": "Ev elektriği" }
    /// </summary>
    [HttpPost("pay")]
    public async Task<IActionResult> Pay([FromBody] PayBillRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _payments.PayBillAsync(userId.Value, request, ct));
    }

    /// <summary>Kayıtlı ödemeyle tek tıkla öde: { "amount": 245.50 }</summary>
    [HttpPost("saved/{id:int}/pay")]
    public async Task<IActionResult> PaySaved(int id, [FromBody] PaySavedRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _payments.PaySavedAsync(userId.Value, id, request, ct));
    }
}
