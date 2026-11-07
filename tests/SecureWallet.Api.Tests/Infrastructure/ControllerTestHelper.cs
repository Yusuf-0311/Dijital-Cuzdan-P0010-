using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace SecureWallet.Api.Tests.Infrastructure;

/// <summary>
/// Controller'ları HTTP sunucusu başlatmadan test etmek için: istenen kullanıcı Id'si
/// JWT'den gelmiş gibi ClaimsPrincipal'a konur ve ProblemDetails üretimi için gerekli
/// servisler hazırlanır.
/// </summary>
public static class ControllerTestHelper
{
    private static readonly IServiceProvider Services = BuildServices();

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        return services.BuildServiceProvider();
    }

    public static T WithUser<T>(this T controller, int? userId) where T : ControllerBase
    {
        var identity = userId is null
            ? new ClaimsIdentity() // kimliği doğrulanmamış istek
            : new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
                RequestServices = Services
            }
        };
        controller.ProblemDetailsFactory = Services.GetRequiredService<ProblemDetailsFactory>();
        return controller;
    }

    /// <summary>Başarılı yanıttaki veriyi tipiyle döndürür.</summary>
    public static TValue OkValue<TValue>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsAssignableFrom<TValue>(ok.Value);
    }

    /// <summary>Hata yanıtının HTTP durum kodunu ve ProblemDetails gövdesini doğrular.</summary>
    public static ProblemDetails Problem(IActionResult result, int expectedStatus)
    {
        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, obj.StatusCode);
        return Assert.IsAssignableFrom<ProblemDetails>(obj.Value);
    }
}
