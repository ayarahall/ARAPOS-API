namespace Ayapos.EndUser.Models.Invoices;

public sealed class InvoiceListItemModel
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Remaining { get; set; }
    public DateTime CreatedAt { get; set; }
}
