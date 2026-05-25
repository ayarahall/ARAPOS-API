namespace Ayapos.Api.Contracts.Cashier;

public sealed class CashierSessionInvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceCode { get; init; } = "";
    public string Status { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public int TotalCents { get; init; }
    public int CollectedCents { get; init; }
    public DateTime CreatedAt { get; init; }
}
