using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Services;

namespace SecureWallet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> CreateWallet(int userId, [FromQuery] string currency = "TRY")
    {
        var wallet = await _walletService.CreateWalletAsync(userId, currency);
        
        return Ok(new 
        { 
            Message = "Cüzdan başarıyla oluşturuldu.", 
            WalletId = wallet.Id, 
            Currency = wallet.Currency 
        });
    }

    [HttpGet("{id}/balance")]
    public async Task<IActionResult> GetBalance(int id)
    {
        var wallet = await _walletService.GetWalletByIdAsync(id);
        
        if (wallet == null)
        {
            return NotFound(new { Message = "Cüzdan bulunamadı." });
        }

        return Ok(new 
        { 
            WalletId = wallet.Id, 
            Balance = wallet.Balance, 
            Currency = wallet.Currency 
        });
    }

    [HttpPost("create-test-user")]
    public async Task<IActionResult> CreateTestUser([FromServices] AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Users\" (\"Id\", \"Username\", \"Email\", \"PasswordHash\", \"CreatedAt\") VALUES (1, 'testuser', 'test@example.com', '123456', CURRENT_TIMESTAMP) ON CONFLICT (\"Id\") DO NOTHING;"
        );
        
        return Ok(new { Message = "1 ID'li test kullanıcısı eklendi (veya zaten mevcuttu). Artık cüzdan oluşturabilirsin." });
    }
}
