namespace Ayapos.EndUser.Models.Services;

public sealed class CreateServiceRequestModel
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int? DurationMin { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public bool IsActive { get; set; } = true;
}
