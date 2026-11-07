namespace SecureWallet.Api.Dtos;

// ---- İstekler ----
public record MoneyRequest(decimal Amount, string? Description);

public record TransferRequest(int ReceiverWalletId, decimal Amount, string? Description);

// ---- Yanıtlar ----
public record WalletDto(int Id, decimal Balance, string Currency);

/// <summary>
/// Hareket listesi ve dekont için işlem özeti.
/// Direction ve BalanceAfter, isteği yapan kullanıcının cüzdanına göre hesaplanır.
/// </summary>
public record TransactionDto(
    string ReferenceNo,
    string Type,
    string Status,
    string Direction,          // "In" (gelen) veya "Out" (giden)
    decimal Amount,
    string Currency,
    string? Description,
    int? SenderWalletId,
    int? ReceiverWalletId,
    decimal? BalanceAfter,     // bu işlemden sonra kullanıcının bakiyesi
    DateTime CreatedAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
