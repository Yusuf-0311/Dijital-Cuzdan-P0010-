using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller desteğini ekle
builder.Services.AddControllers();

// 2. Swagger/OpenAPI dokümantasyonunu ekle (Test edebilmek için)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. PostgreSQL Veritabanı (AppDbContext) bağlantısını kaydet
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. Yazdığımız Wallet Servisi için Dependency Injection (DI) kaydı
builder.Services.AddScoped<IWalletService, WalletService>();

var app = builder.Build();

// 5. HTTP İstek Hattı (Pipeline) Ayarları
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// 6. Controller'ları (Uç Noktaları) eşleştir
app.MapControllers();

app.Run();
