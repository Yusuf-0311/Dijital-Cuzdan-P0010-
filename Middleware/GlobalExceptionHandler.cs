using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SecureWallet.Api.Middleware;

/// <summary>
/// Yakalanmamış tüm hataları loglar ve istemciye stack trace / SQL detayı SIZDIRMADAN
/// standart bir ProblemDetails (500) döndürür. traceId ile log kaydı eşleştirilebilir.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetails;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    {
        _logger = logger;
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // İstemci isteği iptal ettiyse hata sayma.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = 499;
            return true;
        }

        _logger.LogError(exception, "Beklenmeyen hata. TraceId: {TraceId}", httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Beklenmeyen bir hata oluştu",
                Detail = "İşleminiz tamamlanamadı. Lütfen daha sonra tekrar deneyin.",
                Extensions = { ["traceId"] = httpContext.TraceIdentifier }
            }
        });
    }
}
