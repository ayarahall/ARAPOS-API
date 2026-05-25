namespace Ayapos.Api.Contracts.Invoices;

public sealed class UpdateInvoiceLineRequest
{
    public int Qty { get; init; }
    public int? PriceOverrideCents { get; init; }
    public string? PriceOverrideReason { get; init; }
}
