namespace Ayapos.Api.Contracts.Invoices;

public sealed class InvoiceDetailsDto
{
    public Guid Id { get; init; }
    public string InvoiceCode { get; init; } = default!;
    public string Status { get; init; } = default!;
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Total { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal Remaining { get; init; }
    public DateTime CreatedAt { get; init; }
    public InvoicePrintSettingsDto PrintSettings { get; init; } = new();
    public IReadOnlyList<InvoiceLineDto> Items { get; init; } = [];
    public IReadOnlyList<InvoicePaymentDto> Payments { get; init; } = [];
}
