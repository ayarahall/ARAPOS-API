using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid InvoiceId { get; set; }

    public PaymentMethod Method { get; set; }

    public int AmountCents { get; set; }

    public DateTime PaidAt { get; set; }

    public string? Reference { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
