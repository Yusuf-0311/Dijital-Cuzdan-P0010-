namespace SecureWallet.Api.Dtos;

// ---- İstekler ----
public record PayBillRequest(int BillerId, string SubscriberNo, decimal Amount, string? Description, bool SaveForLater = false, string? Nickname = null);

public record PaySavedRequest(decimal Amount, string? Description);

public record CreateSavedPaymentRequest(int BillerId, string SubscriberNo, string Nickname);

// ---- Yanıtlar ----
public record BillerDto(int Id, string Name, string Category, string SubscriberNoLabel);

public record SavedPaymentDto(
    int Id,
    string Nickname,
    int BillerId,
    string BillerName,
    string Category,
    string SubscriberNo,
    int UsageCount,
    DateTime? LastUsedAt);
