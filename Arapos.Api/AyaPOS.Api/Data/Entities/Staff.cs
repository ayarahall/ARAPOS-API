using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class Staff
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid? LinkedUserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? EmployeeCode { get; set; }

    public string? JobTitle { get; set; }

    public string? EmploymentType { get; set; }

    public string? SalaryType { get; set; }

    public decimal? BaseSalary { get; set; }

    public decimal? DeductionPerLateMinute { get; set; }

    public decimal? DeductionPerAbsentDay { get; set; }

    public string? WeeklyOffDays { get; set; }

    public DateTime? HireDate { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Notes { get; set; }

    public bool IsBookableForAppointments { get; set; }

    public bool TrackAttendance { get; set; }

    public bool IsActive { get; set; }

    public string? AppointmentColor { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<StaffAttendance> Attendances { get; set; } = new List<StaffAttendance>();

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<StaffDocument> Documents { get; set; } = new List<StaffDocument>();

    public virtual User? LinkedUser { get; set; }

    public virtual ICollection<StaffLeave> Leaves { get; set; } = new List<StaffLeave>();

    public virtual ICollection<StaffShift> Shifts { get; set; } = new List<StaffShift>();
}
