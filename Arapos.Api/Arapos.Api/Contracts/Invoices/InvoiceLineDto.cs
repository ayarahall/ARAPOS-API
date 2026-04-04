namespace Arapos.Api.Contracts.Invoices;

public sealed class InvoiceLineDto
{
    public Guid Id { get; init; }
    public string ItemType { get; init; } = default!;
    public string Name { get; init; } = default!;
    public int Qty { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public string CurrencyCode { get; init; } = "AED";
}