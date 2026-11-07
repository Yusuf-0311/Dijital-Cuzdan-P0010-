using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

public class PaymentService : IPaymentService
{
    private const int MaxSavedPaymentsPerUser = 20;
    private const int MaxNicknameLength = 50;

    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public PaymentService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // ---------------- Kurumlar ----------------

    public async Task<ServiceResult<IReadOnlyList<BillerDto>>> GetBillersAsync(string? category, CancellationToken ct = default)
    {
        var query = _db.Billers.AsNoTracking().Where(b => b.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<BillerCategory>(category, ignoreCase: true, out var cat) || !Enum.IsDefined(cat))
                return ServiceResult<IReadOnlyList<BillerDto>>.Fail(ErrorCode.Validation, "Geçersiz kategori.");
            query = query.Where(b => b.Category == cat);
        }

        var billers = await query.OrderBy(b => b.Category).ThenBy(b => b.Name).ToListAsync(ct);

        IReadOnlyList<BillerDto> result = billers
            .Select(b => new BillerDto(b.Id, b.Name, b.Category.ToString(), b.SubscriberNoLabel))
            .ToList();

        return ServiceResult<IReadOnlyList<BillerDto>>.Ok(result);
    }

    // ---------------- Kayıtlı (sık kullanılan) ödemeler ----------------

    public async Task<ServiceResult<IReadOnlyList<SavedPaymentDto>>> GetSavedPaymentsAsync(int userId, CancellationToken ct = default)
    {
        var saved = await _db.SavedPayments.AsNoTracking()
            .Where(s => s.UserId == userId)
            .Include(s => s.Biller)
            .OrderByDescending(s => s.UsageCount)
            .ThenByDescending(s => s.LastUsedAt)
            .ThenBy(s => s.Nickname)
            .ToListAsync(ct);

        IReadOnlyList<SavedPaymentDto> result = saved.Select(ToDto).ToList();
        return ServiceResult<IReadOnlyList<SavedPaymentDto>>.Ok(result);
    }

    public async Task<ServiceResult<SavedPaymentDto>> CreateSavedPaymentAsync(int userId, CreateSavedPaymentRequest request, CancellationToken ct = default)
    {
        var subscriberNo = request.SubscriberNo?.Trim() ?? "";
        var nickname = request.Nickname?.Trim() ?? "";

        var error = TransactionRules.ValidateSubscriberNo(subscriberNo) ?? ValidateNickname(nickname);
        if (error is not null)
            return ServiceResult<SavedPaymentDto>.Fail(ErrorCode.Validation, error);

        var biller = await _db.Billers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BillerId && b.IsActive, ct);
        if (biller is null)
            return ServiceResult<SavedPaymentDto>.Fail(ErrorCode.NotFound, "Kurum bulunamadı.");

        var count = await _db.SavedPayments.CountAsync(s => s.UserId == userId, ct);
        if (count >= MaxSavedPaymentsPerUser)
            return ServiceResult<SavedPaymentDto>.Fail(ErrorCode.LimitExceeded, $"En fazla {MaxSavedPaymentsPerUser} kayıtlı ödeme eklenebilir.");

        var exists = await _db.SavedPayments.AnyAsync(
            s => s.UserId == userId && s.BillerId == biller.Id && s.SubscriberNo == subscriberNo, ct);
        if (exists)
            return ServiceResult<SavedPaymentDto>.Fail(ErrorCode.Duplicate, "Bu ödeme zaten kayıtlı.");

        var saved = new SavedPayment
        {
            UserId = userId,
            BillerId = biller.Id,
            SubscriberNo = subscriberNo,
            Nickname = nickname
        };
        _db.SavedPayments.Add(saved);
        _audit.Add(userId, "Kayıtlı Ödeme Eklendi", new { BillerId = biller.Id, Nickname = nickname });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Aynı anda iki istek aynı kaydı eklemeye çalıştıysa unique index yakalar.
            _db.ChangeTracker.Clear();
            return ServiceResult<SavedPaymentDto>.Fail(ErrorCode.Duplicate, "Bu ödeme zaten kayıtlı.");
        }

        // DTO'yu elle kur: AsNoTracking kurumu kayda bağlamak EF'in onu yeni kayıt sanmasına yol açabilir.
        return ServiceResult<SavedPaymentDto>.Ok(new SavedPaymentDto(
            saved.Id, saved.Nickname, biller.Id, biller.Name, biller.Category.ToString(),
            saved.SubscriberNo, saved.UsageCount, saved.LastUsedAt));
    }

    public async Task<ServiceResult<bool>> DeleteSavedPaymentAsync(int userId, int savedPaymentId, CancellationToken ct = default)
    {
        // Başkasının kaydıysa "bulunamadı" de (Id tahminiyle bilgi sızmasın).
        var saved = await _db.SavedPayments.FirstOrDefaultAsync(s => s.Id == savedPaymentId && s.UserId == userId, ct);
        if (saved is null)
            return ServiceResult<bool>.Fail(ErrorCode.NotFound, "Kayıtlı ödeme bulunamadı.");

        _db.SavedPayments.Remove(saved);
        _audit.Add(userId, "Kayıtlı Ödeme Silindi", new { SavedPaymentId = savedPaymentId });
        await _db.SaveChangesAsync(ct);

        return ServiceResult<bool>.Ok(true);
    }

    // ---------------- Ödeme ----------------

    public async Task<ServiceResult<TransactionDto>> PayBillAsync(int userId, PayBillRequest request, CancellationToken ct = default)
    {
        var subscriberNo = request.SubscriberNo?.Trim() ?? "";

        var error = TransactionRules.Validate(request.Amount, request.Description)
                    ?? TransactionRules.ValidateSubscriberNo(subscriberNo);
        if (error is not null)
            return ServiceResult<TransactionDto>.Fail(ErrorCode.Validation, error);

        // Ödemeyle birlikte kaydedilecekse takma ad baştan doğrulanır.
        string? nickname = null;
        if (request.SaveForLater)
        {
            nickname = request.Nickname?.Trim() ?? "";
            var nickError = ValidateNickname(nickname);
            if (nickError is not null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.Validation, nickError);
        }

        var result = await PayInternalAsync(userId, request.BillerId, subscriberNo, request.Amount, request.Description, savedPaymentId: null, ct);

        // Ödeme başarılıysa "sık kullanılanlar"a ekle. Bu adım başarısız olursa (limit, zaten kayıtlı)
        // ödeme geri alınmaz; para gitti, sadece kayıt eklenmedi.
        if (result.Success && request.SaveForLater)
        {
            var already = await _db.SavedPayments.AnyAsync(
                s => s.UserId == userId && s.BillerId == request.BillerId && s.SubscriberNo == subscriberNo, ct);
            if (!already)
                await CreateSavedPaymentAsync(userId, new CreateSavedPaymentRequest(request.BillerId, subscriberNo, nickname!), ct);
        }

        return result;
    }

    public async Task<ServiceResult<TransactionDto>> PaySavedAsync(int userId, int savedPaymentId, PaySavedRequest request, CancellationToken ct = default)
    {
        var error = TransactionRules.Validate(request.Amount, request.Description);
        if (error is not null)
            return ServiceResult<TransactionDto>.Fail(ErrorCode.Validation, error);

        var saved = await _db.SavedPayments.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == savedPaymentId && s.UserId == userId, ct);
        if (saved is null)
            return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Kayıtlı ödeme bulunamadı.");

        return await PayInternalAsync(userId, saved.BillerId, saved.SubscriberNo, request.Amount, request.Description, saved.Id, ct);
    }

    /// <summary>
    /// Ortak ödeme akışı: bakiyeden düş, Payment işlemi oluştur, varsa kayıtlı ödemenin kullanım
    /// sayacını artır, audit yaz. Hepsi tek SaveChanges (tek veritabanı transaction'ı).
    /// </summary>
    private Task<ServiceResult<TransactionDto>> PayInternalAsync(
        int userId, int billerId, string subscriberNo, decimal amount, string? description, int? savedPaymentId, CancellationToken ct)
    {
        return ConcurrencyRetry.RunAsync<TransactionDto>(_db, async () =>
        {
            var biller = await _db.Billers.FirstOrDefaultAsync(b => b.Id == billerId && b.IsActive, ct);
            if (biller is null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Kurum bulunamadı.");

            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
            if (wallet is null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Cüzdan bulunamadı.");

            if (wallet.Balance < amount)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.InsufficientFunds, "Yetersiz bakiye.");

            wallet.Balance -= amount;

            var tx = new Transaction
            {
                ReferenceNo = ReferenceNumber.Generate(),
                Type = TransactionType.Payment,
                Status = TransactionStatus.Completed,
                SenderWalletId = wallet.Id,
                BillerId = biller.Id,
                SubscriberNo = subscriberNo,
                Amount = amount,
                Currency = wallet.Currency,
                Description = TransactionRules.CleanDescription(description),
                SenderBalanceAfter = wallet.Balance
            };
            _db.Transactions.Add(tx);

            // Bu kurum + abone no kayıtlıysa (kayıtlı ödemeden gelinmese bile) kullanım sayacını artır.
            var saved = savedPaymentId is not null
                ? await _db.SavedPayments.FirstOrDefaultAsync(s => s.Id == savedPaymentId && s.UserId == userId, ct)
                : await _db.SavedPayments.FirstOrDefaultAsync(
                    s => s.UserId == userId && s.BillerId == biller.Id && s.SubscriberNo == subscriberNo, ct);
            if (saved is not null)
            {
                saved.UsageCount++;
                saved.LastUsedAt = DateTime.UtcNow;
            }

            _audit.Add(userId, "Fatura Ödendi",
                new { tx.ReferenceNo, tx.Amount, tx.Currency, BillerId = biller.Id, SubscriberNo = subscriberNo });

            await _db.SaveChangesAsync(ct);

            return ServiceResult<TransactionDto>.Ok(TransactionMapper.ToDto(tx, wallet.Id));
        });
    }

    // ---------------- Yardımcılar ----------------

    private static string? ValidateNickname(string nickname) =>
        nickname.Length is 0 or > MaxNicknameLength
            ? $"Kayıt adı 1-{MaxNicknameLength} karakter olmalıdır (örn. \"Ev elektriği\")."
            : null;

    private static SavedPaymentDto ToDto(SavedPayment s) => new(
        s.Id,
        s.Nickname,
        s.BillerId,
        s.Biller?.Name ?? "",
        s.Biller?.Category.ToString() ?? "",
        s.SubscriberNo,
        s.UsageCount,
        s.LastUsedAt);
}
