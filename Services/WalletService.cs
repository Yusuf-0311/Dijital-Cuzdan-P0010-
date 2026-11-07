using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

public class WalletService : IWalletService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public WalletService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private static WalletDto ToDto(Wallet w) => new(w.Id, w.Balance, w.Currency);

    public async Task<ServiceResult<WalletDto>> CreateWalletAsync(int userId, string currency = "TRY", CancellationToken ct = default)
    {
        currency = (currency ?? "").Trim().ToUpperInvariant();
        if (currency.Length != 3)
            return ServiceResult<WalletDto>.Fail(ErrorCode.Validation, "Para birimi 3 harfli bir kod olmalıdır (örn. TRY).");

        if (!await _db.Users.AnyAsync(u => u.Id == userId, ct))
            return ServiceResult<WalletDto>.Fail(ErrorCode.NotFound, "Kullanıcı bulunamadı.");

        var existing = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (existing is not null)
            return ServiceResult<WalletDto>.Ok(ToDto(existing));

        var wallet = new Wallet { UserId = userId, Currency = currency, Balance = 0m };
        _db.Wallets.Add(wallet);
        _audit.Add(userId, "Cüzdan Oluşturuldu", new { Currency = currency });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Aynı anda iki istek cüzdan oluşturmaya çalışmış olabilir (UserId unique).
            _db.ChangeTracker.Clear();
            var raced = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
            if (raced is not null)
                return ServiceResult<WalletDto>.Ok(ToDto(raced));
            throw;
        }

        return ServiceResult<WalletDto>.Ok(ToDto(wallet));
    }

    public async Task<ServiceResult<WalletDto>> GetWalletAsync(int userId, CancellationToken ct = default)
    {
        var wallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
        return wallet is null
            ? ServiceResult<WalletDto>.Fail(ErrorCode.NotFound, "Cüzdan bulunamadı.")
            : ServiceResult<WalletDto>.Ok(ToDto(wallet));
    }

    public Task<ServiceResult<TransactionDto>> DepositAsync(int userId, decimal amount, string? description, CancellationToken ct = default)
    {
        var error = TransactionRules.Validate(amount, description);
        if (error is not null)
            return Task.FromResult(ServiceResult<TransactionDto>.Fail(ErrorCode.Validation, error));

        return ConcurrencyRetry.RunAsync<TransactionDto>(_db, async () =>
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
            if (wallet is null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Cüzdan bulunamadı.");

            wallet.Balance += amount;

            var tx = new Transaction
            {
                ReferenceNo = ReferenceNumber.Generate(),
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                ReceiverWalletId = wallet.Id,
                Amount = amount,
                Currency = wallet.Currency,
                Description = TransactionRules.CleanDescription(description),
                ReceiverBalanceAfter = wallet.Balance
            };
            _db.Transactions.Add(tx);
            _audit.Add(userId, "Para Yatırıldı", new { tx.ReferenceNo, tx.Amount, tx.Currency });

            // Bakiye + işlem kaydı + audit tek SaveChanges = tek veritabanı transaction'ı
            await _db.SaveChangesAsync(ct);

            return ServiceResult<TransactionDto>.Ok(TransactionMapper.ToDto(tx, wallet.Id));
        });
    }

    public Task<ServiceResult<TransactionDto>> WithdrawAsync(int userId, decimal amount, string? description, CancellationToken ct = default)
    {
        var error = TransactionRules.Validate(amount, description);
        if (error is not null)
            return Task.FromResult(ServiceResult<TransactionDto>.Fail(ErrorCode.Validation, error));

        return ConcurrencyRetry.RunAsync<TransactionDto>(_db, async () =>
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
            if (wallet is null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Cüzdan bulunamadı.");

            if (wallet.Balance < amount)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.InsufficientFunds, "Yetersiz bakiye.");

            wallet.Balance -= amount;

            var tx = new Transaction
            {
                ReferenceNo = ReferenceNumber.Generate(),
                Type = TransactionType.Withdraw,
                Status = TransactionStatus.Completed,
                SenderWalletId = wallet.Id,
                Amount = amount,
                Currency = wallet.Currency,
                Description = TransactionRules.CleanDescription(description),
                SenderBalanceAfter = wallet.Balance
            };
            _db.Transactions.Add(tx);
            _audit.Add(userId, "Para Çekildi", new { tx.ReferenceNo, tx.Amount, tx.Currency });

            await _db.SaveChangesAsync(ct);

            return ServiceResult<TransactionDto>.Ok(TransactionMapper.ToDto(tx, wallet.Id));
        });
    }

    public async Task<ServiceResult<PagedResult<TransactionDto>>> GetTransactionsAsync(int userId, TransactionFilter filter, CancellationToken ct = default)
    {
        var wallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wallet is null)
            return ServiceResult<PagedResult<TransactionDto>>.Fail(ErrorCode.NotFound, "Cüzdan bulunamadı.");

        var page = Math.Max(filter.Page ?? 1, 1);
        var pageSize = Math.Clamp(filter.PageSize ?? 20, 1, 100);
        var walletId = wallet.Id;

        var query = _db.Transactions.AsNoTracking()
            .Where(t => t.SenderWalletId == walletId || t.ReceiverWalletId == walletId);

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            if (!Enum.TryParse<TransactionType>(filter.Type, ignoreCase: true, out var type) || !Enum.IsDefined(type))
                return ServiceResult<PagedResult<TransactionDto>>.Fail(
                    ErrorCode.Validation, "Geçersiz işlem tipi. Kullanılabilir: Deposit, Withdraw, Transfer, Payment.");
            query = query.Where(t => t.Type == type);
        }

        if (filter.From is not null && filter.To is not null && filter.From > filter.To)
            return ServiceResult<PagedResult<TransactionDto>>.Fail(ErrorCode.Validation, "Başlangıç tarihi bitiş tarihinden sonra olamaz.");

        if (filter.From is { } from)
        {
            var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt >= fromUtc);
        }
        if (filter.To is { } to)
        {
            var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt <= toUtc);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .WithParties()
            .ToListAsync(ct);

        var items = rows.Select(t => TransactionMapper.ToDto(t, walletId)).ToList();

        return ServiceResult<PagedResult<TransactionDto>>.Ok(new PagedResult<TransactionDto>(items, page, pageSize, total));
    }

    public async Task<ServiceResult<TransactionDto>> GetReceiptAsync(int userId, string referenceNo, CancellationToken ct = default)
    {
        var wallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wallet is null)
            return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Cüzdan bulunamadı.");

        var walletId = wallet.Id;
        var refNo = (referenceNo ?? "").Trim().ToUpperInvariant();

        // İşlemin tarafı değilse "yok" gibi davran (başkasının işlem numarasını keşfetmesin).
        var tx = await _db.Transactions.AsNoTracking()
            .Where(t => t.ReferenceNo == refNo && (t.SenderWalletId == walletId || t.ReceiverWalletId == walletId))
            .WithParties()
            .FirstOrDefaultAsync(ct);

        return tx is null
            ? ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "İşlem bulunamadı.")
            : ServiceResult<TransactionDto>.Ok(TransactionMapper.ToDto(tx, walletId));
    }
}
