using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class ManagerApproval
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid ApprovedByUserId { get; set; }

    public string ApprovalType { get; set; } = null!;

    public string RefType { get; set; } = null!;

    public Guid RefId { get; set; }

    public string? Notes { get; set; }

    public DateTime ApprovedAt { get; set; }
}
