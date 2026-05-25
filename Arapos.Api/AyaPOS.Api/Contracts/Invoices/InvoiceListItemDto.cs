namespace Ayapos.Api.Contracts.Invoices;

public sealed class InvoiceListItemDto
{
    public Guid Id { get; init; }
    public string InvoiceCode { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? CustomerName { get; init; }
    public decimal Total { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal Remaining { get; init; }
    public DateTime CreatedAt { get; init; }
}
