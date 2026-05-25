namespace Ayapos.Api.Contracts.Staff;

public sealed class EmployeeListItemDto
{
    public Guid Id { get; init; }
    public Guid BranchId { get; init; }
    public Guid? LinkedUserId { get; init; }
    public string FullName { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? EmployeeCode { get; init; }
    public string? JobTitle { get; init; }
    public string? EmploymentType { get; init; }
    public string? SalaryType { get; init; }
    public decimal? BaseSalary { get; init; }
    public decimal? DeductionPerLateMinute { get; init; }
    public decimal? DeductionPerAbsentDay { get; init; }
    public string? WeeklyOffDays { get; init; }
    public DateTime? HireDate { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Notes { get; init; }
    public bool IsBookableForAppointments { get; init; }
    public bool TrackAttendance { get; init; }
    public bool IsActive { get; init; }
    public string? AppointmentColor { get; init; }
    public bool HasSystemAccess { get; init; }
    public string? LinkedUsername { get; init; }
    public string? LinkedUserRole { get; init; }
    public string? TodayAttendanceStatus { get; init; }
    public DateTime? TodayCheckInAt { get; init; }
    public DateTime? TodayCheckOutAt { get; init; }
    public decimal TodayDeductionAmount { get; init; }
}
