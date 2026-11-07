using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Extensions;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transfers")]
public class TransferController : ControllerBase
{
    private readonly ITransferService _transfers;

    public TransferController(ITransferService transfers) => _transfers = transfers;

    /// <summary>
    /// Para gönder. Gövde örneği:
    /// { "recipient": "ali@ornek.com", "amount": 150.00, "description": "Kira payı" }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _transfers.TransferAsync(userId.Value, request, ct));
    }

    /// <summary>Göndermeden önce alıcıyı doğrula: /api/transfers/recipient?query=ali@ornek.com</summary>
    [HttpGet("recipient")]
    public async Task<IActionResult> FindRecipient([FromQuery] string query, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _transfers.FindRecipientAsync(userId.Value, query, ct));
    }

    /// <summary>Son para gönderilen kişiler.</summary>
    [HttpGet("recent-recipients")]
    public async Task<IActionResult> RecentRecipients([FromQuery] int take = 5, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _transfers.GetRecentRecipientsAsync(userId.Value, take, ct));
    }
}
