using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Unit;

public class AmountInWordsTests
{
    [Theory]
    [InlineData("150", "Yüz Elli Türk Lirası")]
    [InlineData("1993.26", "Bin Dokuz Yüz Doksan Üç Türk Lirası Yirmi Altı Kuruş")]
    [InlineData("0.50", "Sıfır Türk Lirası Elli Kuruş")]
    [InlineData("1000000", "Bir Milyon Türk Lirası")]
    [InlineData("2001", "İki Bin Bir Türk Lirası")]
    [InlineData("100100", "Yüz Bin Yüz Türk Lirası")]
    [InlineData("11.01", "On Bir Türk Lirası Bir Kuruş")]
    public void Format_WritesTurkishWords(string amount, string expected)
    {
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, AmountInWords.Format(value, "TRY"));
    }

    [Fact]
    public void Format_UsesCurrencyCode_ForOtherCurrencies()
    {
        Assert.Equal("On EUR", AmountInWords.Format(10m, "EUR"));
    }

    [Theory]
    [InlineData(0, "sıfır")]
    [InlineData(1, "bir")]
    [InlineData(1000, "bin")]
    [InlineData(999_999, "dokuz yüz doksan dokuz bin dokuz yüz doksan dokuz")]
    public void Number_HandlesSpecialTurkishRules(long value, string expected)
    {
        Assert.Equal(expected, AmountInWords.Number(value));
    }
}
