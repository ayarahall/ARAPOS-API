using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class InvoiceItem
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid InvoiceId { get; set; }

    public string ItemType { get; set; } = null!;

    public string NameSnapshot { get; set; } = null!;

    public int Qty { get; set; }

    public int UnitPriceCents { get; set; }

    public int LineTotalCents { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public Guid? ProductId { get; set; }

    public Guid? ServiceId { get; set; }

    public int DiscountCents { get; set; }

    public string? DiscountReason { get; set; }

    public int? PriceOverrideCents { get; set; }

    public string? PriceOverrideReason { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public decimal? DiscountPct { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Product? Product { get; set; }

    public virtual Service? Service { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
