namespace Ayapos.EndUser.Models.Cashier;

public sealed class CashierRecentPaymentModel
{
    public Guid PaymentId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; }
}
