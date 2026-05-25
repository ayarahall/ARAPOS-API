namespace Ayapos.Api.Contracts.Staff;

public sealed class EmployeeAttendanceCheckOutRequest
{
    public DateTime? AttendanceDate { get; init; }
    public DateTime? CheckOutAt { get; init; }
    public string? Notes { get; init; }
}
