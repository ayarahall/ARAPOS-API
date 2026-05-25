namespace Ayapos.Api.Contracts.Invoices;

public sealed class SetInvoiceCustomerRequest
{
    public Guid? CustomerId { get; init; }
}
