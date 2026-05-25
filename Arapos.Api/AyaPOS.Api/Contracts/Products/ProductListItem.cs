namespace Ayapos.Api.Contracts.Products;

public sealed class ProductListItem
{
    public required Guid Id { get; init; }
    public Guid? BranchId { get; init; }

    public string? Sku { get; init; }
    public string? Barcode { get; init; }

    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? Unit { get; init; }

    public decimal? SellPrice { get; init; }
    public string? CurrencyCode { get; init; }

    public bool IsActive { get; init; }
    public bool TrackInventory { get; init; }
    public DateTime CreatedAt { get; init; }
}
