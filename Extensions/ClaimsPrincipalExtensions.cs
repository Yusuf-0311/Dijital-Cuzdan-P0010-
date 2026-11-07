using System.Security.Claims;

namespace SecureWallet.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// JWT içindeki kullanıcı Id'sini okur. Token'da kullanıcı Id'si
    /// "sub" veya ClaimTypes.NameIdentifier claim'inde bulunmalıdır.
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
