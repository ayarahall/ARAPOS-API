namespace Ayapos.EndUser.Models.Staff;

public sealed class MarkEmployeeAttendanceRequestModel
{
    public DateTime AttendanceDate { get; set; } = DateTime.Today;
    public string Status { get; set; } = "present";
    public string? Notes { get; set; }
}
