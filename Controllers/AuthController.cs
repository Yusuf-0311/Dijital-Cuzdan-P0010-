using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Extensions;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Yeni kullanıcı kaydı. Başarılıysa 201 ile kullanıcı ve cüzdan bilgisi döner.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);

        return result.Success
            ? StatusCode(StatusCodes.Status201Created, result.Data)
            : this.ToActionResult(result);
    }
}