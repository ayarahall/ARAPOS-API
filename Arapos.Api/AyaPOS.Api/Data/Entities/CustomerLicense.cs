using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class CustomerLicense
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid CustomerId { get; set; }

    public string? LicenseType { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string Status { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }

    public Guid? CreatedInvoiceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Invoice? CreatedInvoice { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
