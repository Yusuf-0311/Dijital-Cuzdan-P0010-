using System.ComponentModel.DataAnnotations;

namespace SecureWallet.Api.Dtos;

/// <summary>
/// Kayıt isteği. Kurallar [ApiController] tarafından otomatik kontrol edilir;
/// biri tutmazsa controller'a hiç girilmeden 400 döner.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [RegularExpression("^[a-zA-Z0-9_]{3,50}$",
        ErrorMessage = "Kullanıcı adı 3-50 karakter olmalı; sadece harf, rakam ve alt çizgi içerebilir.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(256, ErrorMessage = "E-posta en fazla 256 karakter olabilir.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Şifre 8-128 karakter olmalıdır.")]
    [RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).+$",
        ErrorMessage = "Şifre en az bir harf ve bir rakam içermelidir.")]
    public string Password { get; set; } = string.Empty;
}
/// <summary>Kayıt başarılı olunca dönen bilgi. Şifre veya hash ASLA dönülmez.</summary>
public record AuthUserDto(int Id, string Username, string Email, int WalletId);
