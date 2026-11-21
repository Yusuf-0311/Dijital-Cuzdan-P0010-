using System.Text.Json.Serialization;

namespace SecureWallet.Api.Dtos;

// ---- İstekler ----
// Sayısal alanlar [JsonRequired]: istemci alanı göndermezse sessizce 0 olmak yerine
// otomatik 400 Bad Request döner ("under-posting" koruması).

public record MoneyRequest(
    [property: JsonRequired] decimal Amount,
    string? Description);

/// <summary>
/// Recipient: alıcının e-postası veya kullanıcı adı (önerilen).
/// ReceiverWalletId: alternatif olarak doğrudan cüzdan Id'si. İkisinden biri dolu olmalı.
/// </summary>
public record TransferRequest(
    string? Recipient,
    int? ReceiverWalletId,
    [property: JsonRequired] decimal Amount,
    string? Description);

/// <summary>
/// İşlem geçmişi filtresi (query string). Type: Deposit, Withdraw, Transfer, Payment.
/// From/To: UTC tarih. Page/PageSize verilmezse 1 ve 20 kullanılır.
/// </summary>
public record TransactionFilter(string? Type, DateTime? From, DateTime? To, int? Page = null, int? PageSize = null);

// ---- Yanıtlar ----
public record WalletDto(int Id, decimal Balance, string Currency);

/// <summary>Günlük çıkış limiti durumu. ResetsAt: limitin sıfırlanacağı an (UTC, Türkiye saatiyle gece 00:00).</summary>
public record WalletLimitsDto(decimal DailyLimit, decimal UsedToday, decimal RemainingToday, string Currency, DateTime ResetsAt);

/// <summary>
/// Web sitesi ana sayfası için tek çağrıda özet: bakiye, son 5 işlem ve bu ayın gelen/giden toplamı.
/// MonthStart: ay başlangıcı (UTC).
/// </summary>
public record WalletSummaryDto(
    WalletDto Wallet,
    IReadOnlyList<TransactionDto> RecentTransactions,
    decimal MonthIncoming,
    decimal MonthOutgoing,
    DateTime MonthStart);

/// <summary>
/// Hareket listesi ve dekont için işlem özeti.
/// Direction, BalanceAfter ve Counterparty isteği yapan kullanıcının cüzdanına göre hesaplanır.
/// </summary>
public record TransactionDto(
    string ReferenceNo,
    string Type,
    string Status,
    string Direction,          // "In" (gelen) veya "Out" (giden)
    decimal Amount,
    string Currency,
    string? Description,
    string? Counterparty,      // transferde karşı kullanıcı, ödemede kurum adı
    string? SubscriberNo,      // sadece fatura ödemesinde
    int? SenderWalletId,
    int? ReceiverWalletId,
    decimal? BalanceAfter,     // bu işlemden sonra kullanıcının bakiyesi
    DateTime CreatedAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Transfer öncesi "doğru kişiye mi gönderiyorum?" onayı için. İsim maskelenir.</summary>
public record RecipientDto(int WalletId, string MaskedName, string Currency);

public record RecentRecipientDto(int WalletId, string Username, int TransferCount, DateTime LastTransferAt);
