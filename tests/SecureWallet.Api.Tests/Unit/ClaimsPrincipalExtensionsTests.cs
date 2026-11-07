using System.Security.Claims;
using SecureWallet.Api.Extensions;

namespace SecureWallet.Api.Tests.Unit;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test"));

    [Fact]
    public void ReadsUserId_FromNameIdentifier()
    {
        Assert.Equal(42, Principal(new Claim(ClaimTypes.NameIdentifier, "42")).GetUserId());
    }

    [Fact]
    public void ReadsUserId_FromSubClaim()
    {
        Assert.Equal(7, Principal(new Claim("sub", "7")).GetUserId());
    }

    [Fact]
    public void ReturnsNull_WhenClaimMissing()
    {
        Assert.Null(Principal().GetUserId());
    }

    [Fact]
    public void ReturnsNull_WhenClaimIsNotNumber()
    {
        Assert.Null(Principal(new Claim(ClaimTypes.NameIdentifier, "abc")).GetUserId());
    }
}
