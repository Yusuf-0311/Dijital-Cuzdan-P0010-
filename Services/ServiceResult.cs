namespace SecureWallet.Api.Services;

public enum ErrorCode
{
    None,
    Validation,
    NotFound,
    InsufficientFunds,
    CurrencyMismatch,
    SelfTransfer,
    Conflict,
    Duplicate,
    LimitExceeded
}

/// <summary>
/// Servislerin başarı/hata sonucunu exception fırlatmadan döndürmesi için basit sarmalayıcı.
/// Controller bunu HTTP durum koduna çevirir.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; private init; }
    public T? Data { get; private init; }
    public ErrorCode Error { get; private init; }
    public string? Message { get; private init; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };

    public static ServiceResult<T> Fail(ErrorCode code, string message) =>
        new() { Success = false, Error = code, Message = message };
}
