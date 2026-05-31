namespace Ayapos.EndUser.Models.Staff;

public sealed class EmployeeListItemModel
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid? LinkedUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
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
    public bool HasSystemAccess { get; set; }
    public string? LinkedUsername { get; set; }
    public string? LinkedUserRole { get; set; }
    public string? TodayAttendanceStatus { get; set; }
    public DateTime? TodayCheckInAt { get; set; }
    public DateTime? TodayCheckOutAt { get; set; }
    public decimal TodayDeductionAmount { get; set; }
}
