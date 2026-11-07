using SecureWallet.Api.Dtos;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

internal static class TransactionRules
{
    public const decimal MaxAmount = 1_000_000m;     // tek işlem üst limiti (iş kuralı)
    public const int MaxDescriptionLength = 250;     // Transaction.Description sütun uzunluğu

    /// <summary>Geçerliyse null, değilse hata mesajı döndürür.</summary>
    public static string? Validate(decimal amount, string? description)
    {
        if (amount <= 0)
            return "Tutar sıfırdan büyük olmalıdır.";
        if (decimal.Round(amount, 2) != amount)
            return "Tutar en fazla 2 ondalık basamak içerebilir.";
        if (amount > MaxAmount)
            return $"Tek işlem tutarı {MaxAmount} değerini aşamaz.";
        if (description is { Length: > MaxDescriptionLength })
            return $"Açıklama en fazla {MaxDescriptionLength} karakter olabilir.";
        return null;
    }

    public static string? CleanDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}

internal static class ReferenceNumber
{
    /// <summary>Örn: TRX2611073F9A1C0B22 (19 karakter, sütun sınırı 32).</summary>
    public static string Generate() =>
        $"TRX{DateTime.UtcNow:yyMMdd}{Guid.NewGuid():N}"[..19].ToUpperInvariant();
}

internal static class TransactionMapper
{
    public static TransactionDto ToDto(Transaction t, int viewerWalletId)
    {
        var isOutgoing = t.SenderWalletId == viewerWalletId;

        return new TransactionDto(
            t.ReferenceNo,
            t.Type.ToString(),
            t.Status.ToString(),
            isOutgoing ? "Out" : "In",
            t.Amount,
            t.Currency,
            t.Description,
            t.SenderWalletId,
            t.ReceiverWalletId,
            isOutgoing ? t.SenderBalanceAfter : t.ReceiverBalanceAfter,
            t.CreatedAt);
    }
}
