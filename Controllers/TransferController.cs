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

    /// <summary>Başka bir cüzdana para gönder.</summary>
    [HttpPost]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return this.ToActionResult(await _transfers.TransferAsync(
            userId.Value, request.ReceiverWalletId, request.Amount, request.Description, ct));
    }
}
