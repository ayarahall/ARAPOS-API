namespace Ayapos.Api.Contracts.Services;

public sealed class ServiceListItem
{
    public Guid Id { get; init; }

    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public int? DurationMin { get; init; }

    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    // Price from ServicePrices
    public int PriceCents { get; init; }
    public decimal Price { get; init; }
    public string CurrencyCode { get; init; } = "AED";
}