using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class StaffShift
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid StaffId { get; set; }

    public string Name { get; set; } = null!;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int GraceMinutes { get; set; }

    public bool IsActive { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? WeeklyPattern { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<StaffAttendance> Attendances { get; set; } = new List<StaffAttendance>();

    public virtual Branch Branch { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;
}
