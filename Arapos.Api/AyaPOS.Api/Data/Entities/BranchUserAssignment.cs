using System;

namespace Ayapos.Api.Data.Entities;

public partial class BranchUserAssignment
{
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
