namespace SecureWallet.Api.Models;

public enum BillerCategory
{
    Electricity = 1,
    Water = 2,
    NaturalGas = 3,
    Internet = 4,
    MobilePhone = 5,
    Education = 6
}

/// <summary>Fatura/ödeme yapılabilen kurum (elektrik, su, internet vb.).</summary>
public class Biller
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BillerCategory Category { get; set; }

    /// <summary>Arayüzde gösterilecek alan adı, örn. "Abone No", "Telefon No", "Öğrenci No".</summary>
    public string SubscriberNoLabel { get; set; } = "Abone No";

    public bool IsActive { get; set; } = true;
}
