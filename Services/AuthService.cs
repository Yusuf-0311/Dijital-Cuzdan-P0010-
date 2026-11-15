using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SecureWallet.Api.Data;
using SecureWallet.Api.Dtos;
using SecureWallet.Api.Models;

namespace SecureWallet.Api.Services;

public class AuthService : IAuthService
{
    // Kullanıcı bulunamadığında da bir şifre doğrulaması yapmak için sahte hash (aşağıda açıklama).
    private static readonly string DummyHash =
        new PasswordHasher<User>().HashPassword(new User(), "zamanlama-icin-sahte-sifre");

    private readonly AppDbContext _db;
    private readonly IWalletService _wallets;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ITokenService _tokens;
    private readonly IAuditService _audit;

    public AuthService(AppDbContext db, IWalletService wallets, IPasswordHasher<User> hasher,
        ITokenService tokens, IAuditService audit)
    {
        _db = db;
        _wallets = wallets;
        _hasher = hasher;
        _tokens = tokens;
        _audit = audit;
    }

    public async Task<ServiceResult<AuthUserDto>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        // Ön kontrol: kullanıcıya anlaşılır mesaj vermek için. Asıl garanti veritabanındaki unique index.
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return ServiceResult<AuthUserDto>.Fail(ErrorCode.Duplicate, "Bu e-posta adresi zaten kayıtlı.");

        // Kullanıcı adı büyük/küçük harf duyarsız benzersiz olmalı: transferde alıcı
        // kullanıcı adıyla küçük harfe çevrilerek aranıyor ("Kadir" ile "kadir" karışmamalı).
#pragma warning disable CA1862 // EF Core sorgusu: StringComparison SQL'e çevrilemez
        var usernameKey = username.ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Username.ToLower() == usernameKey, ct))
            return ServiceResult<AuthUserDto>.Fail(ErrorCode.Duplicate, "Bu kullanıcı adı alınmış.");
#pragma warning restore CA1862

        // Kullanıcı + cüzdan + denetim kaydı tek transaction: biri başarısız olursa hiçbiri kalmaz.
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var user = new User { Username = username, Email = email };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _db.ChangeTracker.Clear();
            return ServiceResult<AuthUserDto>.Fail(ErrorCode.Duplicate, "Bu e-posta veya kullanıcı adı zaten kayıtlı.");
        }

        var wallet = await _wallets.CreateWalletAsync(user.Id, "TRY", ct);
        if (!wallet.Success)
            return ServiceResult<AuthUserDto>.Fail(wallet.Error, wallet.Message ?? "Cüzdan oluşturulamadı.");

        _audit.Add(user.Id, "Kayıt Olundu", new { user.Username });
        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return ServiceResult<AuthUserDto>.Ok(new AuthUserDto(user.Id, user.Username, user.Email, wallet.Data!.Id));
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Kullanıcı yoksa bile bir şifre doğrulaması yapılır. Yapılmasaydı "kayıtlı olmayan e-posta"
        // cevabı çok daha hızlı dönerdi ve saldırgan cevap süresine bakarak hangi e-postaların
        // kayıtlı olduğunu öğrenebilirdi.
        var verification = _hasher.VerifyHashedPassword(
            user ?? new User(), user?.PasswordHash ?? DummyHash, request.Password);

        if (user is null || verification == PasswordVerificationResult.Failed)
        {
            _audit.Add(user?.Id, "Başarısız Giriş", new { Email = email });
            await _db.SaveChangesAsync(ct);

            // Her iki durumda da AYNI mesaj: "e-posta yok" ile "şifre yanlış" ayırt edilemez.
            return ServiceResult<LoginResponse>.Fail(ErrorCode.InvalidCredentials, "E-posta veya şifre hatalı.");
        }

        // Hash algoritması güncellendiyse (ör. .NET daha güçlü ayara geçtiyse) şifre yeni ayarla yeniden hash'lenir.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _audit.Add(user.Id, "Giriş Yapıldı", new { user.Username });
        await _db.SaveChangesAsync(ct);

        var token = _tokens.CreateAccessToken(user);
        return ServiceResult<LoginResponse>.Ok(new LoginResponse(token.Token, token.ExpiresAt, user.Id, user.Username));
    }
}