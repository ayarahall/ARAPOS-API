namespace Ayapos.Api.Contracts.Staff;

public sealed class CreateEmployeeRequest
{
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
    public bool TrackAttendance { get; init; } = true;
    public Guid? LinkedUserId { get; init; }
    public bool IsActive { get; init; } = true;
    public string? AppointmentColor { get; init; }
}
