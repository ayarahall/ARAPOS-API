using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class Service
{
    public Guid Id { get; set; }

    public string? NameAr { get; set; }

    public string? NameEn { get; set; }

    public int? DurationMin { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? BranchId { get; set; }

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();
}
