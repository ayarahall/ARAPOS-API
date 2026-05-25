namespace Ayapos.Api.Contracts.Cashier;

public sealed class CashierRecentPaymentDto
{
    public Guid PaymentId { get; init; }
    public string InvoiceCode { get; init; } = "";
    public string Method { get; init; } = "";
    public int AmountCents { get; init; }
    public string? Reference { get; init; }
    public DateTime PaidAt { get; init; }
}
