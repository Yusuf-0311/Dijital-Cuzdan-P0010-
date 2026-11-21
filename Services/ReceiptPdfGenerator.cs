using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Services;

public interface IReceiptPdfGenerator
{
    /// <summary>İşlem dekontunu PDF dosyası olarak üretir.</summary>
    /// <param name="transaction">Dekontu istenen işlem (isteği yapan kullanıcının bakış açısıyla).</param>
    /// <param name="ownerName">Dekont sahibinin kullanıcı adı; yoksa "Sayın" satırı yazılmaz.</param>
    byte[] Generate(TransactionDto transaction, string? ownerName = null);
}

/// <summary>Dekontun içeriği: PDF çiziminden ayrı tutulur, böylece birim testleriyle doğrulanabilir.</summary>
internal sealed record ReceiptContent(
    IReadOnlyList<(string Label, string Value)> Header,
    string WalletNumber,
    string Summary,
    string DebitCredit,
    string Currency,
    string Amount,
    string AmountInWords,
    IReadOnlyList<(string Label, string Value)> Details);

/// <summary>
/// Dekont PDF'i (QuestPDF). Banka dekontlarındaki yerleşim örnek alındı: başlık bilgileri,
/// Borç/Alacak sütunlu işlem tablosu, tutar yazıyla ve işlem detayı. Dekontlar başkalarına
/// iletilebildiği için karşı tarafın kullanıcı adı ve abone numarası maskelenir, işlem sonrası
/// bakiye yazılmaz. Yetki kontrolü burada değil, dekontu getiren serviste yapılır.
/// </summary>
public class ReceiptPdfGenerator : IReceiptPdfGenerator
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private const string BrandColor = "#1F4FD8";
    private const string InkColor = "#101828";
    private const string MutedColor = "#475467";
    private const string DividerColor = "#D0D5DD";
    private const string HeaderFill = "#F2F4F7";

    static ReceiptPdfGenerator()
    {
        // QuestPDF Community lisansı: bireysel/öğrenci projeleri için ücretsiz.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(TransactionDto transaction, string? ownerName = null)
    {
        var content = Build(transaction, ownerName);
        var createdAt = FormatDate(DateTime.UtcNow);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(InkColor));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(brand =>
                        {
                            brand.Item().Text("SecureWallet").FontSize(18).Bold().FontColor(BrandColor);
                            brand.Item().Text("Dijital Cüzdan").FontSize(9).FontColor(MutedColor);
                        });
                        row.ConstantItem(220).AlignRight().Text("DEKONT").FontSize(18).Bold();
                    });
                    column.Item().PaddingTop(10).LineHorizontal(1).LineColor(DividerColor);
                });

                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(16);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150);
                            columns.RelativeColumn();
                        });
                        foreach (var (label, value) in content.Header)
                        {
                            table.Cell().PaddingVertical(3).Text(label).FontColor(MutedColor);
                            table.Cell().PaddingVertical(3).Text($": {value}");
                        }
                    });

                    column.Item().Text("Aşağıda cüzdanınızdan gerçekleştirilen işlemin detayları yer almaktadır.");

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(60);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            foreach (var title in new[] { "Cüzdan No", "Açıklama", "B/A", "Para Cinsi" })
                                header.Cell().Background(HeaderFill).Padding(6).Text(title).SemiBold();
                            header.Cell().Background(HeaderFill).Padding(6).AlignRight().Text("Tutar").SemiBold();
                        });

                        table.Cell().BorderBottom(1).BorderColor(DividerColor).Padding(6).Text(content.WalletNumber);
                        table.Cell().BorderBottom(1).BorderColor(DividerColor).Padding(6).Text(content.Summary);
                        table.Cell().BorderBottom(1).BorderColor(DividerColor).Padding(6).Text(content.DebitCredit);
                        table.Cell().BorderBottom(1).BorderColor(DividerColor).Padding(6).Text(content.Currency);
                        table.Cell().BorderBottom(1).BorderColor(DividerColor).Padding(6).AlignRight()
                            .Text(content.Amount).SemiBold();
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150);
                            columns.RelativeColumn();
                        });
                        table.Cell().PaddingVertical(3).Text("Tutar (yazıyla)").FontColor(MutedColor);
                        table.Cell().PaddingVertical(3).Text($": {content.AmountInWords}").SemiBold();
                        table.Cell().PaddingVertical(3).Text("Masraf / Komisyon").FontColor(MutedColor);
                        table.Cell().PaddingVertical(3).Text($": 0,00 {content.Currency}");
                    });

                    if (content.Details.Count > 0)
                    {
                        column.Item().Background(HeaderFill).Padding(10).Column(details =>
                        {
                            details.Spacing(3);
                            details.Item().Text("İŞLEM DETAYI").SemiBold().FontSize(9).FontColor(MutedColor);
                            foreach (var (label, value) in content.Details)
                                details.Item().Text($"{label}: {value}");
                        });
                    }
                });

                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(1).LineColor(DividerColor);
                    column.Item().PaddingTop(6)
                        .Text("Bu dekont SecureWallet tarafından elektronik ortamda oluşturulmuştur. " +
                              "İşlemi uygulamadaki işlem geçmişinden referans numarasıyla doğrulayabilirsiniz.")
                        .FontSize(8).FontColor(MutedColor);
                    column.Item().Text($"Oluşturulma: {createdAt}").FontSize(8).FontColor(MutedColor);
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Dekontun tüm metinlerini işlemden üretir.</summary>
    internal static ReceiptContent Build(TransactionDto tx, string? ownerName)
    {
        var header = new List<(string, string)>
        {
            ("Referans No", tx.ReferenceNo),
            ("İşlem Tarihi ve Saati", FormatDate(tx.CreatedAt)),
            ("İşlem Kanalı", "İnternet (SecureWallet Web)"),
            ("İşlem Türü", TypeLabel(tx)),
            ("Durum", StatusLabel(tx.Status))
        };
        if (!string.IsNullOrWhiteSpace(ownerName))
            header.Insert(1, ("Sayın", ownerName));

        var isOutgoing = tx.Direction == "Out";
        var ownWalletId = isOutgoing ? tx.SenderWalletId : tx.ReceiverWalletId;

        var details = new List<(string, string)>();
        if (tx.Type == "Transfer" && !string.IsNullOrEmpty(tx.Counterparty))
        {
            details.Add((isOutgoing ? "ALICI" : "GÖNDEREN", NameMasker.Mask(tx.Counterparty)));
            var otherWalletId = isOutgoing ? tx.ReceiverWalletId : tx.SenderWalletId;
            if (otherWalletId is { } otherId)
                details.Add((isOutgoing ? "ALICI CÜZDAN NO" : "GÖNDEREN CÜZDAN NO", WalletNumber.Masked(otherId)));
        }
        if (tx.Type == "Payment")
        {
            if (!string.IsNullOrEmpty(tx.Counterparty))
                details.Add(("KURUM", tx.Counterparty));
            if (!string.IsNullOrEmpty(tx.SubscriberNo))
                details.Add(("ABONE NO", MaskSubscriberNo(tx.SubscriberNo)));
        }
        if (!string.IsNullOrWhiteSpace(tx.Description))
            details.Add(("AÇIKLAMA", tx.Description));

        return new ReceiptContent(
            Header: header,
            WalletNumber: ownWalletId is { } id ? WalletNumber.Format(id) : $"{tx.Currency} Cüzdanı",
            Summary: Summary(tx),
            DebitCredit: isOutgoing ? "B" : "A",   // Borç: cüzdandan çıkan, Alacak: cüzdana giren
            Currency: tx.Currency,
            Amount: tx.Amount.ToString("N2", TurkishCulture),
            AmountInWords: AmountInWords.Format(tx.Amount, tx.Currency),
            Details: details);
    }

    /// <summary>UTC zamanı Türkiye saatiyle "22.11.2026 01:30 (TSİ)" biçiminde yazar.</summary>
    internal static string FormatDate(DateTime utc) =>
        TurkeyTime.FromUtc(utc).ToString("dd.MM.yyyy HH:mm", TurkishCulture) + " (TSİ)";

    /// <summary>Son 4 hane görünür: "1234567" → "***4567".</summary>
    internal static string MaskSubscriberNo(string subscriberNo) =>
        subscriberNo.Length <= 4 ? "****" : "***" + subscriberNo[^4..];

    private static string Summary(TransactionDto tx) => tx.Type switch
    {
        "Transfer" when !string.IsNullOrEmpty(tx.Counterparty) =>
            tx.Direction == "Out"
                ? $"{NameMasker.Mask(tx.Counterparty)} kullanıcısına gönderilen"
                : $"{NameMasker.Mask(tx.Counterparty)} tarafından gönderilen",
        "Payment" when !string.IsNullOrEmpty(tx.Counterparty) => $"{tx.Counterparty} fatura ödemesi",
        _ => TypeLabel(tx)
    };

    private static string TypeLabel(TransactionDto tx) => tx.Type switch
    {
        "Deposit" => "Para yatırma",
        "Withdraw" => "Para çekme",
        "Transfer" => tx.Direction == "Out" ? "Giden transfer" : "Gelen transfer",
        "Payment" => "Fatura ödemesi",
        _ => tx.Type
    };

    private static string StatusLabel(string status) => status switch
    {
        "Completed" => "Tamamlandı",
        "Pending" => "Beklemede",
        "Failed" => "Başarısız",
        _ => status
    };
}
