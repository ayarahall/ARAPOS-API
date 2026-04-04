namespace Arapos.Api.Contracts.Invoices;

public sealed class InvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceCode { get; init; } = default!;
    public string Status { get; init; } = default!;
    public decimal Subtotal { get; init; }
    public decimal Total { get; init; }
    public DateTime CreatedAt { get; init; }
}