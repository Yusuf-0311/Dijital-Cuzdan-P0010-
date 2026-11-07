using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Bağlantı dizesi appsettings.json'da DEĞİL; user-secrets veya ortam değişkeninden gelir (bkz. SETUP.md)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection tanımlı değil. SETUP.md dosyasındaki user-secrets adımını uygula.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// TODO (Auth): JWT ayarları (AddAuthentication().AddJwtBearer(...)) burada, builder.Build()'den önce eklenecek.
// TODO (Wallet): builder.Services.AddScoped<IWalletService, WalletService>(); gibi servis kayıtları burada.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// TODO (Auth): app.UseAuthentication(); satırı UseAuthorization'dan ÖNCE gelmeli.
app.UseAuthorization();

app.MapControllers();

app.Run();
