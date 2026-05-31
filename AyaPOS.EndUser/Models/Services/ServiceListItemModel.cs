namespace Ayapos.EndUser.Models.Services;

public sealed class ServiceListItemModel
{
    public Guid Id { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public int? DurationMin { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PriceCents { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "AED";
}
