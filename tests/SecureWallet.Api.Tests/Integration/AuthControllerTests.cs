using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureWallet.Api.Controllers;
using SecureWallet.Api.Data;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Extensions;
using SecureWallet.Api.Models;
using SecureWallet.Api.Services;
using SecureWallet.Api.Tests.Infrastructure;

namespace SecureWallet.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class AuthControllerTests
{
    private readonly PostgresFixture _fx;

    public AuthControllerTests(PostgresFixture fx) => _fx = fx;

    private static AuthController CreateController(AppDbContext db) =>
        new AuthController(new AuthService(
            db, TestData.CreateWalletService(db), new PasswordHasher<User>(),
            new AuditService(db, new HttpContextAccessor())))
        .WithUser(null); // kayıt için giriş gerekmez

    [Fact]
    public async Task Register_Returns201_ThenSameEmailReturns409()
    {
        var name = "k" + Guid.NewGuid().ToString("N")[..10];
        var request = new RegisterRequest { Username = name, Email = $"{name}@test.local", Password = "Sifre1234" };

        await using var db = _fx.CreateDbContext();
        var controller = CreateController(db);

        var created = Assert.IsType<ObjectResult>(await controller.Register(request, CancellationToken.None));
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<AuthUserDto>(created.Value);
        Assert.Equal(name, body.Username);

        ControllerTestHelper.Problem(await controller.Register(request, CancellationToken.None), 409);
    }

    [Fact]
    public void AuthServices_AreRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql("Host=localhost;Database=test;Username=test;Password=test"));
        services.AddWalletServices();
        services.AddAuthServices();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<AuthService>(scope.ServiceProvider.GetRequiredService<IAuthService>());
        Assert.IsType<PasswordHasher<User>>(scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
    }
}