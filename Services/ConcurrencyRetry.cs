using Microsoft.EntityFrameworkCore;
using Npgsql;
using SecureWallet.Api.Data;

namespace SecureWallet.Api.Services;

/// <summary>
/// Optimistic concurrency (Wallet.Version = xmin) çakışmalarında işlemi baştan dener.
/// Her deneme veritabanından güncel bakiyeyi okuyup tekrar hesaplar; böylece
/// aynı anda gelen iki istek bakiyeyi bozamaz.
/// </summary>
internal static class ConcurrencyRetry
{
    private const int MaxAttempts = 3;

    public static async Task<ServiceResult<T>> RunAsync<T>(
        AppDbContext db,
        Func<Task<ServiceResult<T>>> action)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                db.ChangeTracker.Clear(); // yarım kalan değişiklikleri at, sıfırdan oku

                if (attempt >= MaxAttempts)
                {
                    return ServiceResult<T>.Fail(
                        ErrorCode.Conflict,
                        "Cüzdan şu anda başka bir işlem tarafından kullanılıyor. Lütfen tekrar deneyin.");
                }

                await Task.Delay(Random.Shared.Next(20, 80) * attempt);
            }
        }
    }

    private static bool IsRetryable(Exception ex) =>
        ex is DbUpdateConcurrencyException
        || ex is DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure
            }
        };
}
