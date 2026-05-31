namespace Ayapos.EndUser.Models.Invoices;

public sealed class UpdateInvoiceLineRequestModel
{
    public int Qty { get; set; }
    public int? PriceOverrideCents { get; set; }
    public string? PriceOverrideReason { get; set; }
}
