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

    public Task<ServiceResult<TransactionDto>> TransferAsync(
        int senderUserId, int receiverWalletId, decimal amount, string? description, CancellationToken ct = default)
    {
        var error = TransactionRules.Validate(amount, description);
        if (error is not null)
            return Task.FromResult(ServiceResult<TransactionDto>.Fail(ErrorCode.Validation, error));

        return ConcurrencyRetry.RunAsync<TransactionDto>(_db, async () =>
        {
            // Gönderen cüzdan, token'daki kullanıcıdan bulunur; istemci "kimin cüzdanından" seçemez.
            var sender = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == senderUserId, ct);
            if (sender is null)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.NotFound, "Cüzdanınız bulunamadı.");

            if (sender.Id == receiverWalletId)
                return ServiceResult<TransactionDto>.Fail(ErrorCode.SelfTransfer, "Kendi cüzdanınıza transfer yapamazsınız.");

            var receiver = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == receiverWalletId, ct);
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
                Description = TransactionRules.CleanDescription(description),
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

            return ServiceResult<TransactionDto>.Ok(TransactionMapper.ToDto(tx, sender.Id));
        });
    }
}
