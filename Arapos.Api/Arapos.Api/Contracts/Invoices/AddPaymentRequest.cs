using Arapos.Api.Data.Entities;


namespace Arapos.Api.Contracts.Invoices;

public sealed class AddPaymentRequest
{
    public PaymentMethod Method { get; set; }
    public int AmountCents { get; set; }
    public string? Reference { get; set; }
}