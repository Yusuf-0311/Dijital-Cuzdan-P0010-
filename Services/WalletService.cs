using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Models;
using System;
using System.Threading.Tasks;

namespace SecureWallet.Api.Services;

public class WalletService : IWalletService
{
    private readonly AppDbContext _context;

    public WalletService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet> CreateWalletAsync(int userId, string currency)
    {
        var wallet = new Wallet
        {
            UserId = userId,
            Currency = currency,
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();
        
        return wallet;
    }

    public async Task<Wallet?> GetWalletByIdAsync(int id)
    {
        return await _context.Wallets.FindAsync(id);
    }

    public async Task<Wallet> DepositAsync(int walletId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Yatırılacak miktar 0'dan büyük olmalıdır.");

        var wallet = await _context.Wallets.FindAsync(walletId);
        if (wallet == null) throw new Exception("Cüzdan bulunamadı.");

        wallet.Balance += amount;

        // İŞLEM GEÇMİŞİ KAYDI (YATIRMA)
        var transaction = new Transaction
        {
            WalletId = walletId,
            Amount = amount,
            Type = TransactionType.Deposit,
            Description = "Nakit Para Yatırma",
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        return wallet;
    }

    public async Task<Wallet> WithdrawAsync(int walletId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Çekilecek miktar 0'dan büyük olmalıdır.");

        var wallet = await _context.Wallets.FindAsync(walletId);
        if (wallet == null) throw new Exception("Cüzdan bulunamadı.");

        if (wallet.Balance < amount) throw new Exception("Yetersiz bakiye.");

        wallet.Balance -= amount;

        // İŞLEM GEÇMİŞİ KAYDI (ÇEKME)
        var transaction = new Transaction
        {
            WalletId = walletId,
            Amount = amount,
            Type = TransactionType.Withdraw,
            Description = "Nakit Para Çekme",
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        return wallet;
    }
}
