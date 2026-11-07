namespace SecureWallet.Api.Dtos;

// ---- İstekler ----
public record MoneyRequest(decimal Amount, string? Description);

/// <summary>
/// Recipient: alıcının e-postası veya kullanıcı adı (önerilen).
/// ReceiverWalletId: alternatif olarak doğrudan cüzdan Id'si. İkisinden biri dolu olmalı.
/// </summary>
public record TransferRequest(string? Recipient, int? ReceiverWalletId, decimal Amount, string? Description);

/// <summary>İşlem geçmişi filtresi. Type: Deposit, Withdraw, Transfer, Payment. From/To: UTC tarih.</summary>
public record TransactionFilter(string? Type, DateTime? From, DateTime? To, int Page = 1, int PageSize = 20);

// ---- Yanıtlar ----
public record WalletDto(int Id, decimal Balance, string Currency);

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
