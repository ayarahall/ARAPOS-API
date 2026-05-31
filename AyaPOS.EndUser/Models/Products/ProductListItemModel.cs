namespace Ayapos.EndUser.Models.Products;

public sealed class ProductListItemModel
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Unit { get; set; }
    public decimal? SellPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public bool IsActive { get; set; }
    public bool TrackInventory { get; set; }
    public DateTime CreatedAt { get; set; }
}
