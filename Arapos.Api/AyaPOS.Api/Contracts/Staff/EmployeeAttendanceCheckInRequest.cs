namespace Ayapos.Api.Contracts.Staff;

public sealed class EmployeeAttendanceCheckInRequest
{
    public DateTime? AttendanceDate { get; init; }
    public DateTime? CheckInAt { get; init; }
    public string? Notes { get; init; }
}
