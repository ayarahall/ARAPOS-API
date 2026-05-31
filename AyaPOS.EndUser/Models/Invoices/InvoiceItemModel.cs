namespace Ayapos.EndUser.Models.Invoices;

public sealed class InvoiceItemModel
{
    public Guid Id { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string CurrencyCode { get; set; } = "AED";
}
