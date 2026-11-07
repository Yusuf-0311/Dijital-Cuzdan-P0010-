using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Extensions;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Unit;

/// <summary>Servis hata kodlarının doğru HTTP durum kodlarına çevrildiğini doğrular.</summary>
public class ServiceResultExtensionsTests
{
    private sealed class DummyController : ControllerBase { }

    [Fact]
    public void Success_Returns200_WithData()
    {
        var controller = new DummyController().WithUser(1);

        var result = controller.ToActionResult(ServiceResult<string>.Ok("veri"));

        Assert.Equal("veri", ControllerTestHelper.OkValue<string>(result));
    }

    [Theory]
    [InlineData(ErrorCode.Validation, 400)]
    [InlineData(ErrorCode.SelfTransfer, 400)]
    [InlineData(ErrorCode.NotFound, 404)]
    [InlineData(ErrorCode.Conflict, 409)]
    [InlineData(ErrorCode.Duplicate, 409)]
    [InlineData(ErrorCode.InsufficientFunds, 422)]
    [InlineData(ErrorCode.CurrencyMismatch, 422)]
    [InlineData(ErrorCode.LimitExceeded, 422)]
    [InlineData(ErrorCode.None, 500)]
    public void Failure_MapsToExpectedStatusCode(ErrorCode code, int expectedStatus)
    {
        var controller = new DummyController().WithUser(1);

        var result = controller.ToActionResult(ServiceResult<string>.Fail(code, "açıklama"));

        var problem = ControllerTestHelper.Problem(result, expectedStatus);
        Assert.Equal("açıklama", problem.Detail);
        Assert.False(string.IsNullOrEmpty(problem.Title));
    }
}
