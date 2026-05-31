namespace Ayapos.EndUser.Models.Staff;

public class CreateEmployeeRequestModel
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? EmployeeCode { get; set; }
    public string? JobTitle { get; set; }
    public string? EmploymentType { get; set; } = "employee";
    public string? SalaryType { get; set; } = "monthly";
    public decimal? BaseSalary { get; set; }
    public decimal? DeductionPerLateMinute { get; set; }
    public decimal? DeductionPerAbsentDay { get; set; }
    public string? WeeklyOffDays { get; set; }
    public DateTime? HireDate { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsBookableForAppointments { get; set; }
    public bool TrackAttendance { get; set; } = true;
    public Guid? LinkedUserId { get; set; }
    public bool IsActive { get; set; } = true;
}
