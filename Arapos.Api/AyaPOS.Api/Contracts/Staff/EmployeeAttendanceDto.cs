namespace Ayapos.Api.Contracts.Staff;

public sealed class EmployeeAttendanceDto
{
    public Guid Id { get; init; }
    public Guid StaffId { get; init; }
    public Guid? ShiftId { get; init; }
    public DateTime AttendanceDate { get; init; }
    public DateTime? CheckInAt { get; init; }
    public DateTime? CheckOutAt { get; init; }
    public string Status { get; init; } = "";
    public int LateMinutes { get; init; }
    public int WorkedMinutes { get; init; }
    public decimal DeductionAmount { get; init; }
    public string? Notes { get; init; }
    public string? ShiftName { get; init; }
}
