using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class ServicePrice
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid ServiceId { get; set; }

    public int PriceCents { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
