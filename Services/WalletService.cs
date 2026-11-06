using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Models;

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
            Currency = currency.ToUpper(),
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return wallet;
    }

    public async Task<Wallet?> GetWalletByIdAsync(int walletId)
    {
        return await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == walletId);
    }

    public async Task<decimal> GetBalanceAsync(int walletId)
    {
        var wallet = await GetWalletByIdAsync(walletId);
        return wallet?.Balance ?? 0;
    }
}
