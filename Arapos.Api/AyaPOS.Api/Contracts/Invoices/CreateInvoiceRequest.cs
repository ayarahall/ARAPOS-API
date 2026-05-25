namespace Ayapos.Api.Contracts.Invoices;

public sealed class CreateInvoiceRequest
{
    public Guid? CustomerId { get; init; }
}
