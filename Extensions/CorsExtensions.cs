namespace SecureWallet.Api.Extensions;

public static class CorsPolicies
{
    /// <summary>Web sitesinin (tarayıcıdaki arayüzün) API'yi çağırabilmesi için izin politikası.</summary>
    public const string WebClient = "WebClient";
}

public static class CorsExtensions
{
    public const string AllowedOriginsKey = "Cors:AllowedOrigins";

    /// <summary>
    /// Tarayıcı, farklı bir adresten (örn. http://localhost:5173) gelen JavaScript'in API'yi
    /// çağırmasına varsayılan olarak izin vermez. Bu politika SADECE yapılandırmada listelenen
    /// adreslere izin verir. Liste boşsa hiçbir adrese izin verilmez (güvenli varsayılan).
    /// "*" (herkese izin) bilinçli olarak reddedilir.
    /// </summary>
    public static IServiceCollection AddWebClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? Array.Empty<string>();

        foreach (var origin in origins)
            EnsureValidOrigin(origin);

        services.AddCors(options => options.AddPolicy(CorsPolicies.WebClient, policy =>
        {
            if (origins.Length == 0)
                return; // hiçbir adres eklenmezse politika tüm çapraz istekleri reddeder

            policy.WithOrigins(origins)
                  .WithHeaders("Authorization", "Content-Type")   // JWT ve JSON gövdesi
                  .WithMethods("GET", "POST", "DELETE")            // API'nin kullandığı yöntemler
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));   // tarayıcı izin kontrolünü 10 dk önbellekler
        }));

        return services;
    }

    private static void EnsureValidOrigin(string origin)
    {
        if (origin.Trim() == "*")
            throw new InvalidOperationException($"{AllowedOriginsKey}: '*' kullanılamaz; izin verilecek adresleri tek tek yazın.");

        var valid = Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    && origin.TrimEnd('/') == $"{uri.Scheme}://{uri.Authority}";

        if (!valid)
            throw new InvalidOperationException(
                $"{AllowedOriginsKey}: '{origin}' geçerli bir adres değil. Örnek: http://localhost:5173 (yol ve sorgu olmadan).");
    }
}
