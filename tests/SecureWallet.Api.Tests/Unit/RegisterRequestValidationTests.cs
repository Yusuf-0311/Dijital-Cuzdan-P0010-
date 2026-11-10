using System.ComponentModel.DataAnnotations;
using SecureWallet.Api.Dtos;

namespace SecureWallet.Api.Tests.Unit;

public class RegisterRequestValidationTests
{
    private static List<ValidationResult> Validate(RegisterRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static RegisterRequest Valid() => new()
    {
        Username = "kadir_01",
        Email = "kadir@test.local",
        Password = "Sifre1234"
    };

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        Assert.Empty(Validate(Valid()));
    }

    [Theory]
    [InlineData("ab")]                 // çok kısa
    [InlineData("kadir yilmaz")]       // boşluk
    [InlineData("kadir@x")]            // özel karakter
    [InlineData("")]                   // boş
    public void InvalidUsername_IsRejected(string username)
    {
        var request = Valid();
        request.Username = username;

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(RegisterRequest.Username)));
    }

    [Theory]
    [InlineData("kadir")]              // @ yok
    [InlineData("")]
    public void InvalidEmail_IsRejected(string email)
    {
        var request = Valid();
        request.Email = email;

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(RegisterRequest.Email)));
    }

    [Theory]
    [InlineData("Kisa1")]              // 8'den kısa
    [InlineData("sadeceharf")]         // rakam yok
    [InlineData("12345678")]           // harf yok
    [InlineData("")]
    public void WeakPassword_IsRejected(string password)
    {
        var request = Valid();
        request.Password = password;

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void TooLongPassword_IsRejected()
    {
        var request = Valid();
        request.Password = new string('a', 128) + "1";

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(RegisterRequest.Password)));
    }
}