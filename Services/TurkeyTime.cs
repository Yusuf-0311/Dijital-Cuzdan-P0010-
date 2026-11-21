namespace SecureWallet.Api.Services;

/// <summary>
/// Veritabanında zamanlar UTC tutulur; kullanıcıya gösterilen tarihler ve "gün" sınırı
/// Türkiye saatine göredir. Saat dilimi tek yerden bulunur ve her yerde aynısı kullanılır.
/// </summary>
internal static class TurkeyTime
{
    public static readonly TimeZoneInfo Zone = FindZone();

    /// <summary>UTC zamanı Türkiye saatine çevirir.</summary>
    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    private static TimeZoneInfo FindZone()
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
