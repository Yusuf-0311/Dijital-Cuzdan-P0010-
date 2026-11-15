using Microsoft.EntityFrameworkCore;
using SecureWallet.Api.Data;
using SecureWallet.Api.Extensions;

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
builder.Services.AddSwaggerWithJwt();

builder.Services.AddGlobalErrorHandling();
builder.Services.AddMoneyRateLimiting();
builder.Services.AddWalletServices();
builder.Services.AddAuthServices();
builder.Services.AddWebClientCors(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(); // en başta: sonraki tüm middleware'lerin hatalarını yakalar

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicies.WebClient); // web sitesinin API'yi çağırabilmesi için; kimlik doğrulamadan ÖNCE
app.UseAuthentication();             // token'ı okuyup kullanıcıyı tanır
app.UseAuthorization();              // [Authorize] olan uç noktalara kimliksiz isteği reddeder
app.UseRateLimiter();                // kimlik doğrulamadan sonra: limit kullanıcı başına uygulanır

app.MapControllers();

await app.RunAsync();