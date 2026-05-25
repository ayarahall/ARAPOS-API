using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class Tenant
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string LicensePlan { get; set; } = null!;

    public string LicenseStatus { get; set; } = null!;

    public int MaxUsers { get; set; }

    public DateTime LicenseStartedAt { get; set; }

    public DateTime LicenseExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();

    public virtual ICollection<CustomerLicense> CustomerLicenses { get; set; } = new List<CustomerLicense>();

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<InvoiceSequence> InvoiceSequences { get; set; } = new List<InvoiceSequence>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();
}
