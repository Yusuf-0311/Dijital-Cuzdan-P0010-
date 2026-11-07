using System.Text.Json;
using SecureWallet.Api.Data;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

public interface IAuditService
{
    /// <summary>Kaydı sadece context'e ekler; çağıran SaveChanges ile işlemle birlikte kaydeder (atomik).</summary>
    void Add(int? userId, string action, object details);

    /// <summary>Kaydı ekler ve hemen kaydeder (örn. başarısız giriş denemesi için).</summary>
    Task LogAsync(int? userId, string action, object details, CancellationToken ct = default);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public void Add(int? userId, string action, object details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Details = JsonSerializer.Serialize(details), // DB sütunu jsonb: geçerli JSON olmalı
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task LogAsync(int? userId, string action, object details, CancellationToken ct = default)
    {
        Add(userId, action, details);
        await _db.SaveChangesAsync(ct);
    }
}
