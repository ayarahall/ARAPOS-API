namespace Ayapos.EndUser.Models.Products;

public sealed class CreateProductRequestModel
{
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Unit { get; set; } = "pcs";
    public decimal SellPrice { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public bool IsActive { get; set; } = true;
    public bool TrackInventory { get; set; } = true;
}
