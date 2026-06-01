namespace Ayapos.Api.Contracts.Platform;

public sealed class ProductImportRowDto
{
    public int RowNumber { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public decimal? Price { get; init; }
    public decimal? CostPrice { get; init; }
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public string? Unit { get; init; }
    public string CurrencyCode { get; init; } = "AED";
}
