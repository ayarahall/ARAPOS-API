namespace Ayapos.Api.Contracts.Invoices;

public sealed class InvoicePaymentDto
{
    public Guid Id { get; init; }
    public string Method { get; init; } = default!;
    public decimal Amount { get; init; }
    public string? Reference { get; init; }
    public DateTime PaidAt { get; init; }
}
