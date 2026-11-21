using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Unit;

public class WalletNumberTests
{
    [Theory]
    [InlineData(1, "SW 0000 0018")]
    [InlineData(42, "SW 0000 0422")]
    [InlineData(1_234_567, "SW 1234 5674")]
    public void Format_AddsPrefixPaddingAndLuhnCheckDigit(int walletId, string expected)
    {
        Assert.Equal(expected, WalletNumber.Format(walletId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_000_000)]
    public void Format_RejectsOutOfRangeIds(int walletId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WalletNumber.Format(walletId));
    }

    [Theory]
    [InlineData("SW 0000 0422")]
    [InlineData("SW00000422")]
    [InlineData("sw0000-0422")]
    [InlineData("  SW 0000 0422  ")]
    public void TryParse_AcceptsCommonWritingStyles(string input)
    {
        Assert.True(WalletNumber.TryParse(input, out var walletId));
        Assert.Equal(42, walletId);
    }

    [Theory]
    [InlineData("SW 0000 0423")]   // kontrol hanesi yanlış (tek hane hatası)
    [InlineData("SW 0000 4022")]   // iki hane yer değiştirmiş
    [InlineData("TR 0000 0422")]   // önek yanlış
    [InlineData("SW 0000 042")]    // eksik hane
    [InlineData("SW 0000 04A2")]   // harf
    [InlineData("SW 0000 0000")]   // Id 0
    [InlineData("kadir")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_RejectsInvalidNumbers(string? input)
    {
        Assert.False(WalletNumber.TryParse(input, out _));
    }

    [Fact]
    public void Masked_ShowsOnlyLastFourDigits()
    {
        Assert.Equal("SW **** 0422", WalletNumber.Masked(42));
    }

    [Fact]
    public void FormatAndParse_RoundTrip()
    {
        foreach (var id in new[] { 1, 7, 99, 1_000, 123_456, 9_999_999 })
        {
            Assert.True(WalletNumber.TryParse(WalletNumber.Format(id), out var parsed));
            Assert.Equal(id, parsed);
        }
    }
}
