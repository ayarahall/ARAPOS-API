using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Arapos.Api.Data.Entities;

public partial class Invoice
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public int InvoiceNo { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public Guid? CustomerId { get; set; }

    public string Status { get; set; } = null!;

    public int SubtotalCents { get; set; }

    public int TaxCents { get; set; }

    public int DiscountCents { get; set; }

    public int TotalCents { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? IssuedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Branch BranchNavigation { get; set; } = null!;

    public virtual ICollection<CustomerLicense> CustomerLicenses { get; set; } = new List<CustomerLicense>();

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Tenant Tenant { get; set; } = null!;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}
