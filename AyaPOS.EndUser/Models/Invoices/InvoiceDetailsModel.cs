namespace Ayapos.EndUser.Models.Invoices;

public sealed class InvoiceDetailsModel
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Remaining { get; set; }
    public DateTime CreatedAt { get; set; }
    public InvoicePrintSettingsModel PrintSettings { get; set; } = new();
    public List<InvoiceItemModel> Items { get; set; } = [];
    public List<InvoicePaymentModel> Payments { get; set; } = [];
}
