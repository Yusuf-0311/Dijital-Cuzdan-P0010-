using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

public interface IWalletService
{
    Task<Wallet> CreateWalletAsync(int userId, string currency);
    Task<Wallet?> GetWalletByIdAsync(int walletId);
    Task<decimal> GetBalanceAsync(int walletId);
}
