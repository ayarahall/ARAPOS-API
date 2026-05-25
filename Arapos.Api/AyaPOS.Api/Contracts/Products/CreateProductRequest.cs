namespace Ayapos.Api.Contracts.Products;

public sealed class CreateProductRequest
{
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? Unit { get; init; }
    public decimal SellPrice { get; init; }
    public string? CurrencyCode { get; init; }
    public bool IsActive { get; init; } = true;
    public bool TrackInventory { get; init; } = true;
}
