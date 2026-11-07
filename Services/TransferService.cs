using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

public class TransferService : ITransferService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public TransferService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ServiceResult<TransactionDto>> TransferAsync(int senderUserId, TransferRequest request, CancellationToken ct = default)
    {
        var error = TransactionRules.Validate(request.Amount, request.Description);
        if (error is not null)
            return ServiceResult<TransactionDto>.Fail(ErrorCode.Validation, error);

        // Alıcıyı bir kez çözümle (e-posta / kullanıcı adı / cüzdan Id).
        int receiverWalletId;
        if (!string.IsNullOrWhiteSpace(request.Recipient))
        {
            var found = await FindWalletIdAsync(request.Recipient, ct);
            if (found is null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Alıcı bulunamadı.");
            receiverWalletId = found.Value;
        }
        else if (request.ReceiverWalletId is { } id)
        {
            receiverWalletId = id;
        }
        else
        {
            return ServiceResult<TransactionDto>.Fail(ErrorCode.Validation, "Alıcı (e-posta, kullanıcı adı veya cüzdan Id) belirtilmelidir.");
        }

        var amount = request.Amount;

        return await ConcurrencyRetry.RunAsync<TransactionDto>(_db, async () =>
        {
            // Gönderen cüzdan, token'daki kullanıcıdan bulunur; istemci "kimin cüzdanından" seçemez.
            var sender = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == senderUserId, ct);
            if (sender is null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Cüzdanınız bulunamadı.");

            if (sender.Id == receiverWalletId)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.SelfTransfer, "Kendi cüzdanınıza transfer yapamazsınız.");

            var receiver = await _db.Wallets
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.Id == receiverWalletId, ct);
            if (receiver is null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Alıcı cüzdan bulunamadı.");

            if (sender.Currency != receiver.Currency)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.CurrencyMismatch, "Cüzdanların para birimleri farklı.");

            if (sender.Balance < amount)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.InsufficientFunds, "Yetersiz bakiye.");

            sender.Balance -= amount;
            receiver.Balance += amount;

            var tx = new Transaction
            {
                ReferenceNo = ReferenceNumber.Generate(),
                Type = TransactionType.Transfer,
                Status = TransactionStatus.Completed,
                SenderWalletId = sender.Id,
                ReceiverWalletId = receiver.Id,
                Amount = amount,
                Currency = sender.Currency,
                Description = TransactionRules.CleanDescription(request.Description),
                SenderBalanceAfter = sender.Balance,
                ReceiverBalanceAfter = receiver.Balance
            };
            _db.Transactions.Add(tx);
            _audit.Add(senderUserId, "Para Transfer Edildi",
                new { tx.ReferenceNo, tx.Amount, tx.Currency, SenderWalletId = sender.Id, ReceiverWalletId = receiver.Id });

            // İki bakiye güncellemesi + işlem kaydı + audit TEK SaveChanges = ya hepsi ya hiçbiri.
            // Cüzdanlardan biri arada başka işlemle değiştiyse (xmin) DbUpdateConcurrencyException
            // fırlar ve ConcurrencyRetry işlemi güncel bakiyelerle baştan dener.
            await _db.SaveChangesAsync(ct);

            // tx.ReceiverWallet (User dahil) EF tarafından bağlandığı için dekontta alıcı adı görünür.
            return ServiceResult<TransactionDto>.Ok(TransactionMapper.ToDto(tx, sender.Id));
        });
    }

    public async Task<ServiceResult<RecipientDto>> FindRecipientAsync(int senderUserId, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ServiceResult<RecipientDto>.Fail(ErrorCode.Validation, "E-posta veya kullanıcı adı girilmelidir.");

        var match = await WalletsMatching(query)
            .Select(w => new { w.Id, w.UserId, w.Currency, w.User!.Username })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ServiceResult<RecipientDto>.Fail(ErrorCode.NotFound, "Alıcı bulunamadı.");

        if (match.UserId == senderUserId)
            return ServiceResult<RecipientDto>.Fail(ErrorCode.SelfTransfer, "Kendi cüzdanınıza transfer yapamazsınız.");

        return ServiceResult<RecipientDto>.Ok(new RecipientDto(match.Id, NameMasker.Mask(match.Username), match.Currency));
    }

    public async Task<ServiceResult<IReadOnlyList<RecentRecipientDto>>> GetRecentRecipientsAsync(int userId, int take = 5, CancellationToken ct = default)
    {
        var wallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wallet is null)
            return ServiceResult<IReadOnlyList<RecentRecipientDto>>.Fail(ErrorCode.NotFound, "Cüzdan bulunamadı.");

        take = Math.Clamp(take, 1, 20);
        var walletId = wallet.Id;

        var recent = await _db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Transfer
                        && t.SenderWalletId == walletId
                        && t.Status == TransactionStatus.Completed)
            .GroupBy(t => t.ReceiverWalletId)
            .Select(g => new { WalletId = g.Key!.Value, Count = g.Count(), Last = g.Max(t => t.CreatedAt) })
            .OrderByDescending(x => x.Last)
            .Take(take)
            .ToListAsync(ct);

        var ids = recent.Select(r => r.WalletId).ToList();
        var names = await _db.Wallets.AsNoTracking()
            .Where(w => ids.Contains(w.Id))
            .Select(w => new { w.Id, w.User!.Username })
            .ToDictionaryAsync(w => w.Id, w => w.Username, ct);

        IReadOnlyList<RecentRecipientDto> result = recent
            .Where(r => names.ContainsKey(r.WalletId))
            .Select(r => new RecentRecipientDto(r.WalletId, names[r.WalletId], r.Count, r.Last))
            .ToList();

        return ServiceResult<IReadOnlyList<RecentRecipientDto>>.Ok(result);
    }

    private Task<int?> FindWalletIdAsync(string recipient, CancellationToken ct) =>
        WalletsMatching(recipient)
            .Select(w => (int?)w.Id)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// E-posta veya kullanıcı adı (büyük/küçük harf duyarsız) ile cüzdan arar.
    /// </summary>
    /// <remarks>
    /// Bilinçli karar: Kod analizi (CA1862) burada string.Equals(..., StringComparison.OrdinalIgnoreCase)
    /// önerir; ancak bu ifade LINQ-to-Entities sorgusu içindedir ve EF Core onu SQL'e ÇEVİREMEZ
    /// (çalışma anında InvalidOperationException). ToLower() ise PostgreSQL'de lower() olarak çevrilir.
    /// Kural bellek içi string karşılaştırması için geçerli olduğundan burada bastırıldı.
    /// </remarks>
    private IQueryable<Wallet> WalletsMatching(string recipient)
    {
        var key = recipient.Trim().ToLowerInvariant();
#pragma warning disable CA1862 // EF Core sorgusu: StringComparison SQL'e çevrilemez (bkz. remarks)
        return _db.Wallets.AsNoTracking()
            .Where(w => w.User!.Email.ToLower() == key || w.User!.Username.ToLower() == key);
#pragma warning restore CA1862
    }
}
