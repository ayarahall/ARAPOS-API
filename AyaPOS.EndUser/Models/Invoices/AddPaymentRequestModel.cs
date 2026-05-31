namespace Ayapos.EndUser.Models.Invoices;

public sealed class AddPaymentRequestModel
{
    public int Method { get; set; } = 1;
    public int AmountCents { get; set; }
    public string? Reference { get; set; }
}
