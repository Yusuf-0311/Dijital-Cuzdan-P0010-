using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecureWallet.Api.Models;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Extensions;

public static class AuthServiceCollectionExtensions
{
    /// <summary>Kimlik doğrulama servislerini kaydeder.</summary>
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    /// <summary>
    /// Gelen her istekteki JWT'yi doğrular: imza (Key), veren (Issuer), hedef (Audience) ve süre.
    /// Key tanımlı değilse uygulama hiç açılmaz; böylece "anahtarsız" çalışan bir sunucu olmaz.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(JwtOptions.SectionName);
        var jwt = section.Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key tanımlı değil veya 32 karakterden kısa. user-secrets ile ayarlayın (bkz. SETUP.md).");
        if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
            throw new InvalidOperationException("Jwt:Issuer ve Jwt:Audience appsettings.json'da tanımlı olmalı.");

        services.AddOptions<JwtOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30) // sunucu saatleri arasındaki küçük farklara tolerans
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>Swagger'a "Authorize" butonu ekler: token yapıştırılınca korumalı uç noktalar denenebilir.</summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Girişte dönen accessToken değerini yapıştırın (başına 'Bearer' yazmayın)."
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }
}