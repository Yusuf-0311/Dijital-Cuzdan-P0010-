using System.Text.Json;
using SecureWallet.Api.Data;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

public interface IAuditService
{
    /// <summary>
    /// Denetim kaydını sadece context'e ekler. Çağıran servis SaveChanges ile işlemle
    /// BİRLİKTE kaydeder; böylece işlem başarısız olursa yarım audit kaydı kalmaz.
    /// </summary>
    void Add(int? userId, string action, object details);
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
}
