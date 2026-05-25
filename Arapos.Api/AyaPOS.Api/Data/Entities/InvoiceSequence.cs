using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class InvoiceSequence
{
    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public int NextNumber { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Branch BranchNavigation { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
