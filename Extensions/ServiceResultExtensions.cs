using Microsoft.AspNetCore.Mvc;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Extensions;

public static class ServiceResultExtensions
{
    /// <summary>ServiceResult'ı HTTP yanıtına çevirir (başarıda 200 + veri, hatada ProblemDetails).</summary>
    public static IActionResult ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        if (result.Success)
            return controller.Ok(result.Data);

        var (status, title) = result.Error switch
        {
            ErrorCode.Validation => (StatusCodes.Status400BadRequest, "Geçersiz istek"),
            ErrorCode.SelfTransfer => (StatusCodes.Status400BadRequest, "Geçersiz transfer"),
            ErrorCode.NotFound => (StatusCodes.Status404NotFound, "Bulunamadı"),
            ErrorCode.InsufficientFunds => (StatusCodes.Status422UnprocessableEntity, "Yetersiz bakiye"),
            ErrorCode.CurrencyMismatch => (StatusCodes.Status422UnprocessableEntity, "Para birimi uyuşmazlığı"),
            ErrorCode.Conflict => (StatusCodes.Status409Conflict, "Çakışma"),
            _ => (StatusCodes.Status500InternalServerError, "Hata")
        };

        return controller.Problem(detail: result.Message, statusCode: status, title: title);
    }
}
