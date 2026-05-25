namespace Ayapos.Api.Contracts.Invoices;

public sealed class AddInvoiceLineRequest
{
    public string ItemType { get; init; } = default!;
    public Guid ItemId { get; init; }
    public int Qty { get; init; }
    public int? PriceOverrideCents { get; init; }
    public string? PriceOverrideReason { get; init; }
}