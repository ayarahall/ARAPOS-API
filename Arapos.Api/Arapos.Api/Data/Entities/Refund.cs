using System.ComponentModel.DataAnnotations;

namespace Arapos.Api.Data.Entities;

public class Refund
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid InvoiceId { get; set; }

    public Guid PaymentId { get; set; }

    public int AmountCents { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime RefundedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}