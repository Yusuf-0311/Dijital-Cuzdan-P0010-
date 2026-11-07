using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Unit;

/// <summary>Veritabanı gerektirmeyen saf iş kuralı testleri.</summary>
public class TransactionRulesTests
{
    [Theory]
    [InlineData("0.01")]
    [InlineData("100")]
    [InlineData("999999.99")]
    [InlineData("1000000")]
    public void Validate_AcceptsValidAmounts(string amount)
    {
        Assert.Null(TransactionRules.Validate(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), null));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("10.001")]      // 2'den fazla ondalık
    [InlineData("1000000.01")]  // üst limit aşımı
    public void Validate_RejectsInvalidAmounts(string amount)
    {
        Assert.NotNull(TransactionRules.Validate(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), null));
    }

    [Fact]
    public void Validate_RejectsTooLongDescription()
    {
        var longText = new string('a', TransactionRules.MaxDescriptionLength + 1);
        Assert.NotNull(TransactionRules.Validate(10m, longText));
    }

    [Theory]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    [InlineData("  Kira  ", "Kira")]
    public void CleanDescription_TrimsOrNulls(string? input, string? expected)
    {
        Assert.Equal(expected, TransactionRules.CleanDescription(input));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345678901234567890")]
    public void ValidateSubscriberNo_AcceptsDigits(string value)
    {
        Assert.Null(TransactionRules.ValidateSubscriberNo(value));
    }

    [Theory]
    [InlineData("1234")]                    // çok kısa
    [InlineData("123456789012345678901")]   // çok uzun
    [InlineData("12A456")]                  // harf
    [InlineData("123 456")]                 // boşluk
    [InlineData("")]
    [InlineData(null)]
    public void ValidateSubscriberNo_RejectsInvalid(string? value)
    {
        Assert.NotNull(TransactionRules.ValidateSubscriberNo(value));
    }

    [Theory]
    [InlineData("yusuf", "yu***")]
    [InlineData("ab", "a***")]
    [InlineData("a", "a***")]
    [InlineData("", "***")]
    public void NameMasker_HidesMostOfName(string input, string expected)
    {
        Assert.Equal(expected, NameMasker.Mask(input));
    }

    [Fact]
    public void ReferenceNumber_HasExpectedFormat_AndIsUnique()
    {
        var refs = Enumerable.Range(0, 1000).Select(_ => ReferenceNumber.Generate()).ToList();

        Assert.All(refs, r =>
        {
            Assert.StartsWith("TRX", r);
            Assert.Equal(19, r.Length);
            Assert.True(r.Length <= 32, "Veritabanı sütun sınırı 32 karakter");
        });
        Assert.Equal(refs.Count, refs.Distinct().Count());
    }
}
