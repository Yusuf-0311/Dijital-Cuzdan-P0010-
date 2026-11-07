using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SecureWallet.Api.Middleware;

namespace SecureWallet.Api.Tests.Unit;

/// <summary>Beklenmeyen hatalarda iç detayların istemciye sızmadığını doğrular.</summary>
public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, DefaultHttpContext Context) Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();

        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            provider.GetRequiredService<IProblemDetailsService>());

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();
        return (handler, context);
    }

    [Fact]
    public async Task UnexpectedException_Returns500_WithoutLeakingDetails()
    {
        var (handler, context) = Create();

        var handled = await handler.TryHandleAsync(
            context, new InvalidOperationException("gizli-sql-detayi: SELECT * FROM Users"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Contains("traceId", body);
        Assert.DoesNotContain("gizli-sql-detayi", body);
        Assert.DoesNotContain("SELECT", body);
    }

    [Fact]
    public async Task ClientCancelledRequest_IsNotTreatedAsServerError()
    {
        var (handler, context) = Create();
        context.RequestAborted = new CancellationToken(canceled: true);

        var handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(499, context.Response.StatusCode);
    }
}
