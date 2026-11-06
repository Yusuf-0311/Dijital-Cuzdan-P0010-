using System.Threading.Tasks;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

public interface IWalletService
{
    Task<Wallet> CreateWalletAsync(int userId, string currency);
    Task<Wallet?> GetWalletByIdAsync(int id);
    
    // YENİ: Para Yatırma ve Çekme tanımları
    Task<Wallet> DepositAsync(int walletId, decimal amount);
    Task<Wallet> WithdrawAsync(int walletId, decimal amount);
}
