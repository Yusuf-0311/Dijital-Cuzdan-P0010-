using System.Globalization;

namespace SecureWallet.Api.Services;

/// <summary>
/// Kullanıcıya gösterilen cüzdan numarası: "SW 0000 0422" (SW + 7 haneli cüzdan Id + 1 kontrol hanesi).
/// Veritabanında ayrı sütun yoktur; numara cüzdan Id'sinden türetilir ve geri çözülür.
/// Kontrol hanesi Luhn algoritmasıyla (kredi kartlarındaki gibi) hesaplanır: elle yazarken yapılan
/// tek hane hatası veya yan yana iki hanenin yer değiştirmesi yakalanır, para yanlış cüzdana gitmez.
/// </summary>
internal static class WalletNumber
{
    private const string Prefix = "SW";
    private const int IdDigits = 7;
    private const int MaxWalletId = 9_999_999;

    /// <summary>Cüzdan Id'sinden görüntülenecek numarayı üretir: 42 → "SW 0000 0422".</summary>
    public static string Format(int walletId)
    {
        if (walletId is <= 0 or > MaxWalletId)
            throw new ArgumentOutOfRangeException(nameof(walletId), "Cüzdan Id'si 1 ile 9.999.999 arasında olmalıdır.");

        var body = walletId.ToString("D7", CultureInfo.InvariantCulture);
        var digits = body + CheckDigit(body);
        return $"{Prefix} {digits[..4]} {digits[4..]}";
    }

    /// <summary>Son 4 hane görünür: "SW **** 0422". Dekontta karşı tarafın numarası için.</summary>
    public static string Masked(int walletId) => $"{Prefix} **** {Format(walletId)[^4..]}";

    /// <summary>
    /// Kullanıcının yazdığı numarayı çözer. Boşluk, tire ve küçük harf kabul edilir
    /// ("sw0000-0422"). Biçim veya kontrol hanesi hatalıysa false döner.
    /// </summary>
    public static bool TryParse(string? input, out int walletId)
    {
        walletId = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var compact = new string(input.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();
        if (compact.Length != Prefix.Length + IdDigits + 1 || !compact.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        var digits = compact[Prefix.Length..];
        if (!digits.All(char.IsAsciiDigit))
            return false;

        var body = digits[..IdDigits];
        if (digits[IdDigits] != CheckDigit(body))
            return false;

        walletId = int.Parse(body, CultureInfo.InvariantCulture);
        return walletId > 0;
    }

    /// <summary>Luhn kontrol hanesi: sağdan başlayarak her ikinci hane iki katına çıkarılır.</summary>
    internal static char CheckDigit(string body)
    {
        var sum = 0;
        for (var i = 0; i < body.Length; i++)
        {
            var digit = body[body.Length - 1 - i] - '0';
            if (i % 2 == 0)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }
            sum += digit;
        }
        return (char)('0' + (10 - sum % 10) % 10);
    }
}
