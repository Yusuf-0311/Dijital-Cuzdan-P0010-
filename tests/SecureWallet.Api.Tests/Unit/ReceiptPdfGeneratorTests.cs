using SecureWallet.Api.Dtos;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Tests.Unit;

public class ReceiptPdfGeneratorTests
{
    private static TransactionDto Tx(
        string type = "Transfer", string direction = "Out", decimal amount = 1_993.26m,
        string? counterparty = "yusuf", string? subscriberNo = null, string? description = "Kira payı") =>
        new(ReferenceNo: "TRX2611220ABCDEF12",
            Type: type,
            Status: "Completed",
            Direction: direction,
            Amount: amount,
            Currency: "TRY",
            Description: description,
            Counterparty: counterparty,
            SubscriberNo: subscriberNo,
            SenderWalletId: 1,
            ReceiverWalletId: type == "Transfer" ? 2 : null,
            BalanceAfter: 98_765.43m,
            CreatedAt: new DateTime(2026, 11, 21, 22, 30, 0, DateTimeKind.Utc));

    [Fact]
    public void Generate_ProducesPdfFile()
    {
        var bytes = new ReceiptPdfGenerator().Generate(Tx(), "kadir");

        Assert.True(bytes.Length > 1_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Header_HasOwnerDateChannelTypeAndStatus()
    {
        var content = ReceiptPdfGenerator.Build(Tx(), "kadir");

        Assert.Contains(("Referans No", "TRX2611220ABCDEF12"), content.Header);
        Assert.Contains(("Sayın", "kadir"), content.Header);
        Assert.Contains(("İşlem Tarihi ve Saati", "22.11.2026 01:30 (TSİ)"), content.Header); // 22:30 UTC = 01:30 TSİ
        Assert.Contains(("İşlem Kanalı", "İnternet (SecureWallet Web)"), content.Header);
        Assert.Contains(("İşlem Türü", "Giden transfer"), content.Header);
        Assert.Contains(("Durum", "Tamamlandı"), content.Header);
    }

    [Fact]
    public void Header_WithoutOwnerName_SkipsSayinLine()
    {
        var content = ReceiptPdfGenerator.Build(Tx(), ownerName: null);

        Assert.DoesNotContain(content.Header, h => h.Label == "Sayın");
    }

    [Fact]
    public void OutgoingTransfer_IsDebit_AndRecipientIsMasked()
    {
        var content = ReceiptPdfGenerator.Build(Tx(), "kadir");

        Assert.Equal("B", content.DebitCredit);
        Assert.Equal("SW 0000 0018", content.WalletNumber);             // kendi cüzdanı (Id 1), tam
        Assert.Contains(("ALICI CÜZDAN NO", "SW **** 0026"), content.Details); // karşı taraf (Id 2), maskeli
        Assert.Equal("1.993,26", content.Amount);
        Assert.Equal("Bin Dokuz Yüz Doksan Üç Türk Lirası Yirmi Altı Kuruş", content.AmountInWords);
        Assert.Equal("yu*** kullanıcısına gönderilen", content.Summary);
        Assert.Contains(("ALICI", "yu***"), content.Details);
        Assert.Contains(("AÇIKLAMA", "Kira payı"), content.Details);
    }

    [Fact]
    public void IncomingTransfer_IsCredit_AndShowsSender()
    {
        var content = ReceiptPdfGenerator.Build(Tx(direction: "In"), "kadir");

        Assert.Equal("A", content.DebitCredit);
        Assert.Contains(("GÖNDEREN", "yu***"), content.Details);
        Assert.Equal("SW 0000 0026", content.WalletNumber);             // gelen transferde kendi cüzdanı alıcı (Id 2)
        Assert.Contains(("GÖNDEREN CÜZDAN NO", "SW **** 0018"), content.Details);
        Assert.Contains(("İşlem Türü", "Gelen transfer"), content.Header);
    }

    [Fact]
    public void BillPayment_ShowsBiller_AndMasksSubscriberNo()
    {
        var content = ReceiptPdfGenerator.Build(
            Tx(type: "Payment", counterparty: "Kent Elektrik Dağıtım", subscriberNo: "1234567", description: null), "kadir");

        Assert.Equal("Kent Elektrik Dağıtım fatura ödemesi", content.Summary);
        Assert.Contains(("KURUM", "Kent Elektrik Dağıtım"), content.Details);
        Assert.Contains(("ABONE NO", "***4567"), content.Details);
        Assert.DoesNotContain(content.Details, d => d.Label == "AÇIKLAMA");
    }

    [Fact]
    public void Receipt_NeverContainsFullCounterpartyOrBalance()
    {
        var content = ReceiptPdfGenerator.Build(Tx(), "kadir");
        var allText = string.Join(" ", content.Header.Concat(content.Details).Select(r => r.Value))
                      + " " + content.Summary;

        Assert.DoesNotContain("yusuf", allText);
        Assert.DoesNotContain("98.765", allText); // işlem sonrası bakiye dekontta yok
    }

    [Theory]
    [InlineData("1234567", "***4567")]
    [InlineData("1234", "****")]
    public void SubscriberNo_ShowsOnlyLastFourDigits(string input, string expected)
    {
        Assert.Equal(expected, ReceiptPdfGenerator.MaskSubscriberNo(input));
    }
}
