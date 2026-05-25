using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

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

    public virtual ICollection<BranchUserAssignment> BranchUserAssignments { get; set; } = new List<BranchUserAssignment>();

    public virtual ICollection<Invoice> InvoiceBranchNavigations { get; set; } = new List<Invoice>();

    public virtual ICollection<Invoice> InvoiceBranches { get; set; } = new List<Invoice>();

    public virtual InvoiceSequence? InvoiceSequenceBranchNavigation { get; set; }

    public virtual ICollection<InvoiceSequence> InvoiceSequenceBranches { get; set; } = new List<InvoiceSequence>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();

    public virtual ICollection<Staff> StaffMembers { get; set; } = new List<Staff>();

    public virtual ICollection<StaffAttendance> StaffAttendances { get; set; } = new List<StaffAttendance>();

    public virtual ICollection<StaffDocument> StaffDocuments { get; set; } = new List<StaffDocument>();

    public virtual ICollection<StaffLeave> StaffLeaves { get; set; } = new List<StaffLeave>();

    public virtual ICollection<StaffShift> StaffShifts { get; set; } = new List<StaffShift>();

    public virtual Tenant Tenant { get; set; } = null!;
}
