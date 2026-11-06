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
            Balance = 0, // Cüzdan her zaman 0 bakiye ile açılır
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

    // YENİ: Para Yatırma
    public async Task<Wallet> DepositAsync(int walletId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Yatırılacak miktar 0'dan büyük olmalıdır.");

        var wallet = await _context.Wallets.FindAsync(walletId);
        if (wallet == null) throw new Exception("Cüzdan bulunamadı.");

        wallet.Balance += amount;
        await _context.SaveChangesAsync();

        return wallet;
    }

    // YENİ: Para Çekme
    public async Task<Wallet> WithdrawAsync(int walletId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Çekilecek miktar 0'dan büyük olmalıdır.");

        var wallet = await _context.Wallets.FindAsync(walletId);
        if (wallet == null) throw new Exception("Cüzdan bulunamadı.");

        // Yetersiz bakiye kontrolü
        if (wallet.Balance < amount) throw new Exception("Yetersiz bakiye.");

        wallet.Balance -= amount;
        await _context.SaveChangesAsync();

        return wallet;
    }
}
