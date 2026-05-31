namespace Ayapos.EndUser.Models.Cashier;

public sealed class CashierSessionInvoiceModel
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int TotalCents { get; set; }
    public int CollectedCents { get; set; }
    public DateTime CreatedAt { get; set; }
}
