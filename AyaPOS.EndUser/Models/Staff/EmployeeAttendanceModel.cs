namespace Ayapos.EndUser.Models.Staff;

public sealed class EmployeeAttendanceModel
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int LateMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public decimal DeductionAmount { get; set; }
    public string? Notes { get; set; }
    public string? ShiftName { get; set; }
}
