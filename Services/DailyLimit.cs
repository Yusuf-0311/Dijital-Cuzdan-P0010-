using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

/// <summary>
/// Cüzdan limitleri. Değer appsettings.json'daki "WalletLimits" bölümünden okunur;
/// ayar yoksa varsayılan kullanılır. Uygulama açılırken geçerliliği kontrol edilir.
/// </summary>
public class WalletLimitOptions
{
    public const string SectionName = "WalletLimits";
    public const decimal DefaultDailyOutgoingLimit = 100_000m;

    /// <summary>Bir cüzdandan bir günde çıkabilecek toplam tutar (para çekme + giden transfer).</summary>
    public decimal DailyOutgoingLimit { get; set; } = DefaultDailyOutgoingLimit;
}

/// <summary>
/// Günlük çıkış limiti hesapları. Hesap ele geçirilse bile cüzdanın bir günde
/// boşaltılmasını engeller. Fatura ödemeleri sayılmaz: para sabit kurumlara gider.
/// </summary>
internal static class DailyLimit
{
    private static readonly TimeZoneInfo Turkey = FindTurkeyTimeZone();
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// "Bugün"ün başlangıcı: Türkiye saatiyle 00:00'ın UTC karşılığı.
    /// Veritabanında zamanlar UTC tutulduğu için gün sınırı burada çevrilir;
    /// yoksa gece 00:00–03:00 arasındaki işlemler bir önceki güne sayılırdı.
    /// </summary>
    public static DateTime DayStartUtc(DateTime nowUtc)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Turkey);
        var localMidnight = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, Turkey);
    }

    /// <summary>Cüzdandan bugün çıkan toplam (tamamlanmış para çekme + giden transfer). SUM veritabanında hesaplanır.</summary>
    public static async Task<decimal> UsedTodayAsync(AppDbContext db, int walletId, DateTime dayStartUtc, CancellationToken ct) =>
        await db.Transactions
            .Where(t => t.SenderWalletId == walletId
                        && t.Status == TransactionStatus.Completed
                        && t.CreatedAt >= dayStartUtc
                        && (t.Type == TransactionType.Withdraw || t.Type == TransactionType.Transfer))
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

    /// <summary>İşlem limite sığıyorsa null, sığmıyorsa kalan limiti içeren hata mesajı döndürür.</summary>
    public static string? Check(decimal usedToday, decimal amount, decimal limit, string currency)
    {
        if (usedToday + amount <= limit)
            return null;

        var remaining = Math.Max(0m, limit - usedToday);
        return $"Günlük çıkış limiti aşılıyor. Bugün kalan limit: {remaining.ToString("N2", TurkishCulture)} {currency}.";
    }

    private static TimeZoneInfo FindTurkeyTimeZone()
    {
        // Linux/macOS IANA kimliği, Windows kendi kimliğini kullanır.
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Bu sistemde bu kimlik yok; sıradakini dene.
            }
            catch (InvalidTimeZoneException)
            {
                // Saat dilimi verisi bozuk; sıradakini dene.
            }
        }

        // Saat dilimi verisi olmayan ortamlar (ör. bazı Docker imajları) için:
        // Türkiye 2016'dan beri yaz saati uygulamıyor, sabit UTC+3.
        return TimeZoneInfo.CreateCustomTimeZone("Turkey", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
    }
}
