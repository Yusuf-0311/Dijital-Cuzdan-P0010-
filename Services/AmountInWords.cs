using System.Globalization;

namespace SecureWallet.Api.Services;

/// <summary>
/// Tutarı Türkçe yazıya çevirir (dekontlarda standart alan):
/// 1.993,26 TRY → "Bin Dokuz Yüz Doksan Üç Türk Lirası Yirmi Altı Kuruş".
/// Tek işlem üst sınırı 1.000.000 olduğu için milyar basamağı gerekmez.
/// </summary>
internal static class AmountInWords
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly string[] Ones = { "", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz" };
    private static readonly string[] Tens = { "", "on", "yirmi", "otuz", "kırk", "elli", "altmış", "yetmiş", "seksen", "doksan" };

    public static string Format(decimal amount, string currency)
    {
        var rounded = Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);
        var whole = (long)Math.Truncate(rounded);
        var cents = (int)((rounded - whole) * 100);

        var unit = currency == "TRY" ? "Türk Lirası" : currency;
        var text = $"{Number(whole)} {unit}";
        if (cents > 0)
            text += $" {Number(cents)} Kuruş";

        return TitleCase(text);
    }

    /// <summary>0–999.999.999 arası tam sayıyı yazıya çevirir ("bir bin" değil "bin", "bir yüz" değil "yüz").</summary>
    internal static string Number(long value)
    {
        if (value == 0)
            return "sıfır";

        var parts = new List<string>();
        var millions = value / 1_000_000;
        var thousands = value / 1_000 % 1_000;
        var rest = value % 1_000;

        if (millions > 0)
            parts.Add($"{Hundreds((int)millions)} milyon");
        if (thousands == 1)
            parts.Add("bin");
        else if (thousands > 1)
            parts.Add($"{Hundreds((int)thousands)} bin");
        if (rest > 0)
            parts.Add(Hundreds((int)rest));

        return string.Join(" ", parts);
    }

    private static string Hundreds(int value)
    {
        var words = new List<string>();
        var hundreds = value / 100;
        if (hundreds > 1)
            words.Add(Ones[hundreds]);
        if (hundreds > 0)
            words.Add("yüz");
        if (Tens[value / 10 % 10].Length > 0)
            words.Add(Tens[value / 10 % 10]);
        if (Ones[value % 10].Length > 0)
            words.Add(Ones[value % 10]);
        return string.Join(" ", words);
    }

    private static string TitleCase(string text) =>
        string.Join(" ", text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpper(w[0], TurkishCulture) + w[1..]));
}
