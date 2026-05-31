namespace Ayapos.EndUser.Models.Invoices;

public sealed class InvoicePaymentModel
{
    public Guid Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; }
}
