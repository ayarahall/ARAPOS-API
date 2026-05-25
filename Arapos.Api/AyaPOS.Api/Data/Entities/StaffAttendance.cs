using System;

namespace Ayapos.Api.Data.Entities;

public partial class StaffAttendance
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid StaffId { get; set; }

    public Guid? ShiftId { get; set; }

    public DateTime AttendanceDate { get; set; }

    public DateTime? CheckInAt { get; set; }

    public DateTime? CheckOutAt { get; set; }

    public string Status { get; set; } = null!;

    public int LateMinutes { get; set; }

    public int WorkedMinutes { get; set; }

    public decimal DeductionAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;

    public virtual StaffShift? Shift { get; set; }
}
