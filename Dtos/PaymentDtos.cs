using System.Text.Json.Serialization;

namespace SecureWallet.Api.Dtos;

// ---- İstekler ----
// Sayısal alanlar [JsonRequired]: eksik gönderilirse 0 kabul edilmez, 400 döner.
// SaveForLater isteğe bağlı olduğu için nullable (gönderilmezse "kaydetme").

public record PayBillRequest(
    [property: JsonRequired] int BillerId,
    string SubscriberNo,
    [property: JsonRequired] decimal Amount,
    string? Description,
    bool? SaveForLater = null,
    string? Nickname = null);

public record PaySavedRequest(
    [property: JsonRequired] decimal Amount,
    string? Description);

public record CreateSavedPaymentRequest(
    [property: JsonRequired] int BillerId,
    string SubscriberNo,
    string Nickname);

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
