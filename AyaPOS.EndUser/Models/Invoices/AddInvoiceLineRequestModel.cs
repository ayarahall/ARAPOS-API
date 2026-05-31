namespace Ayapos.EndUser.Models.Invoices;

public sealed class AddInvoiceLineRequestModel
{
    public string ItemType { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public int Qty { get; set; }
    public int? PriceOverrideCents { get; set; }
    public string? PriceOverrideReason { get; set; }
}
