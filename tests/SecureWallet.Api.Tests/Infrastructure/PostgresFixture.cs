using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using Testcontainers.PostgreSql;

namespace SecureWallet.Api.Tests.Infrastructure;

/// <summary>
/// Tüm entegrasyon testleri için tek bir PostgreSQL konteyneri başlatır ve migration'ları uygular.
/// InMemory provider yerine gerçek PostgreSQL kullanılır; çünkü CHECK kısıtları, unique index'ler,
/// xmin eşzamanlılık kontrolü ve transaction davranışı ancak gerçek veritabanında test edilebilir.
/// Gereksinim: Docker çalışıyor olmalı.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:15")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync(); // migration dosyalarını da test etmiş oluyoruz
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Her çağrıda yeni bir DbContext (gerçek uygulamadaki istek başına scope gibi).</summary>
    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options);
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
