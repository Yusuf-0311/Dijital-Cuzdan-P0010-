using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureWallet.Api.Controllers;
using SecureWallet.Api.Extensions;

namespace SecureWallet.Api.Tests.Unit;

public class AuthRateLimitTests
{
    [Fact]
    public void SameIp_IsRejected_AfterPermitLimit()
    {
        var partition = AuthRateLimitExtensions.CreateAuthPartition(ContextFrom("10.0.0.1"));
        using var limiter = partition.Factory(partition.PartitionKey);

        for (var i = 0; i < AuthRateLimitPolicies.PermitLimit; i++)
            Assert.True(limiter.AttemptAcquire().IsAcquired);

        Assert.False(limiter.AttemptAcquire().IsAcquired); // 11. istek reddedilir
    }

    [Fact]
    public void DifferentIps_GetSeparateLimits()
    {
        var first = AuthRateLimitExtensions.CreateAuthPartition(ContextFrom("10.0.0.1"));
        var second = AuthRateLimitExtensions.CreateAuthPartition(ContextFrom("10.0.0.2"));

        Assert.NotEqual(first.PartitionKey, second.PartitionKey);
    }

    [Fact]
    public void MissingIp_FallsBackToSharedKey()
    {
        var partition = AuthRateLimitExtensions.CreateAuthPartition(new DefaultHttpContext());

        Assert.Equal("unknown", partition.PartitionKey);
    }

    [Theory]
    [InlineData(nameof(AuthController.Register))]
    [InlineData(nameof(AuthController.Login))]
    [InlineData(nameof(AuthController.ChangePassword))]
    public void AuthEndpoints_UseAuthRateLimitPolicy(string action)
    {
        var attribute = typeof(AuthController).GetMethod(action)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(AuthRateLimitPolicies.Auth, attribute!.PolicyName);
    }

    [Fact]
    public void AddAuthRateLimiting_RegistersRateLimiterConfiguration()
    {
        var services = new ServiceCollection();

        services.AddAuthRateLimiting();

        Assert.Contains(services, d => d.ServiceType == typeof(IConfigureOptions<RateLimiterOptions>));
    }

    private static DefaultHttpContext ContextFrom(string ip)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return context;
    }
}
