using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class Branch
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string CurrencyCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual BranchSetting? BranchSetting { get; set; }

    public virtual ICollection<Invoice> InvoiceBranchNavigations { get; set; } = new List<Invoice>();

    public virtual ICollection<Invoice> InvoiceBranches { get; set; } = new List<Invoice>();

    public virtual InvoiceSequence? InvoiceSequenceBranchNavigation { get; set; }

    public virtual ICollection<InvoiceSequence> InvoiceSequenceBranches { get; set; } = new List<InvoiceSequence>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();

    public virtual Tenant Tenant { get; set; } = null!;
}
