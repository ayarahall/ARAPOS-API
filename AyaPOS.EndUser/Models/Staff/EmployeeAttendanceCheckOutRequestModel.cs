namespace Ayapos.EndUser.Models.Staff;

public sealed class EmployeeAttendanceCheckOutRequestModel
{
    public DateTime? AttendanceDate { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string? Notes { get; set; }
}
