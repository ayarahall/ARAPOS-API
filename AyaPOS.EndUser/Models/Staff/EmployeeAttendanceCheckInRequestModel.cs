namespace Ayapos.EndUser.Models.Staff;

public sealed class EmployeeAttendanceCheckInRequestModel
{
    public DateTime? AttendanceDate { get; set; }
    public DateTime? CheckInAt { get; set; }
    public string? Notes { get; set; }
}
