using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SecureWallet.Api.Data;

/// <summary>
/// "dotnet ef" komutları (migrations add / database update) için tasarım zamanı fabrikası.
/// Bağlantı dizesini appsettings.json, user-secrets ve ortam değişkenlerinden okur.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .AddEnvironmentVariables() // ConnectionStrings__DefaultConnection
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection bulunamadı. SETUP.md dosyasındaki user-secrets adımını uygula.");
        }

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(connectionString);

        return new AppDbContext(builder.Options);
    }
}
