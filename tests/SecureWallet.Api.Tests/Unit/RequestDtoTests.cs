using System.Text.Json;
using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Tests.Unit;

/// <summary>
/// "Under-posting" koruması: zorunlu sayısal alan gönderilmezse istek sessizce 0 ile
/// işlenmek yerine reddedilmeli. ASP.NET Core bu JsonException'ı otomatik 400'e çevirir.
/// </summary>
public class RequestDtoTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(typeof(MoneyRequest), """{ "description": "tutar yok" }""")]
    [InlineData(typeof(TransferRequest), """{ "recipient": "ali@test.local" }""")]
    [InlineData(typeof(PayBillRequest), """{ "subscriberNo": "123456", "amount": 10 }""")]   // billerId yok
    [InlineData(typeof(PayBillRequest), """{ "billerId": 1, "subscriberNo": "123456" }""")]  // amount yok
    [InlineData(typeof(PaySavedRequest), """{ }""")]
    [InlineData(typeof(CreateSavedPaymentRequest), """{ "subscriberNo": "123456", "nickname": "Ev" }""")]
    public void MissingRequiredNumber_IsRejected(Type requestType, string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, requestType, Web));
    }

    [Fact]
    public void CompleteRequest_IsAccepted_AndOptionalFieldsDefault()
    {
        var request = JsonSerializer.Deserialize<PayBillRequest>(
            """{ "billerId": 1, "subscriberNo": "123456", "amount": 245.50 }""", Web)!;

        Assert.Equal(1, request.BillerId);
        Assert.Equal(245.50m, request.Amount);
        Assert.Null(request.SaveForLater);
    }
}
