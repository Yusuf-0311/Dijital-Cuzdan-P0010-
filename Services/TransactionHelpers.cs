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

    /// <summary>Abone/tesisat/telefon no: boşluksuz, 5-20 haneli sayı.</summary>
    public static string? ValidateSubscriberNo(string? subscriberNo)
    {
        var value = subscriberNo?.Trim() ?? "";
        if (value.Length is < 5 or > 20 || !value.All(char.IsAsciiDigit))
            return "Abone numarası 5-20 haneli ve sadece rakamlardan oluşmalıdır.";
        return null;
    }
}

internal static class ReferenceNumber
{
    /// <summary>Örn: TRX2611073F9A1C0B22 (19 karakter, sütun sınırı 32).</summary>
    public static string Generate() =>
        $"TRX{DateTime.UtcNow:yyMMdd}{Guid.NewGuid():N}"[..19].ToUpperInvariant();
}

internal static class NameMasker
{
    /// <summary>"yusuf" -> "yu***". Alıcıyı doğrulatırken kullanıcı adını tam ifşa etmemek için.</summary>
    public static string Mask(string name)
    {
        if (string.IsNullOrEmpty(name)) return "***";
        var visible = name.Length <= 2 ? 1 : 2;
        return name[..visible] + "***";
    }
}

internal static class TransactionMapper
{
    /// <summary>
    /// Karşı taraf adı için gerekli ilişkiler. Geçmiş/dekont sorgularında Include yerine kullanılır.
    /// </summary>
    public static IQueryable<Transaction> WithParties(this IQueryable<Transaction> query) =>
        query
            .Select(t => new Transaction
            {
                Id = t.Id,
                ReferenceNo = t.ReferenceNo,
                Type = t.Type,
                Status = t.Status,
                SenderWalletId = t.SenderWalletId,
                ReceiverWalletId = t.ReceiverWalletId,
                BillerId = t.BillerId,
                SubscriberNo = t.SubscriberNo,
                Amount = t.Amount,
                Currency = t.Currency,
                Description = t.Description,
                SenderBalanceAfter = t.SenderBalanceAfter,
                ReceiverBalanceAfter = t.ReceiverBalanceAfter,
                CreatedAt = t.CreatedAt,
                // Sadece kullanıcı adı çekilir; PasswordHash vb. belleğe hiç gelmez.
                SenderWallet = t.SenderWallet == null ? null : new Wallet
                {
                    Id = t.SenderWallet.Id,
                    User = new User { Username = t.SenderWallet.User!.Username }
                },
                ReceiverWallet = t.ReceiverWallet == null ? null : new Wallet
                {
                    Id = t.ReceiverWallet.Id,
                    User = new User { Username = t.ReceiverWallet.User!.Username }
                },
                Biller = t.Biller == null ? null : new Biller { Id = t.Biller.Id, Name = t.Biller.Name }
            });

    public static TransactionDto ToDto(Transaction t, int viewerWalletId)
    {
        var isOutgoing = t.SenderWalletId == viewerWalletId;

        string? counterparty = t.Type switch
        {
            TransactionType.Payment => t.Biller?.Name,
            TransactionType.Transfer => isOutgoing
                ? t.ReceiverWallet?.User?.Username
                : t.SenderWallet?.User?.Username,
            _ => null
        };

        return new TransactionDto(
            t.ReferenceNo,
            t.Type.ToString(),
            t.Status.ToString(),
            isOutgoing ? "Out" : "In",
            t.Amount,
            t.Currency,
            t.Description,
            counterparty,
            t.SubscriberNo,
            t.SenderWalletId,
            t.ReceiverWalletId,
            isOutgoing ? t.SenderBalanceAfter : t.ReceiverBalanceAfter,
            t.CreatedAt);
    }
}
