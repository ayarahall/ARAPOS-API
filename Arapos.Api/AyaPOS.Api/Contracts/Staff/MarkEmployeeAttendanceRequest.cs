namespace Ayapos.Api.Contracts.Staff;

public sealed class MarkEmployeeAttendanceRequest
{
    public DateTime AttendanceDate { get; init; }
    public string Status { get; init; } = "";
    public string? Notes { get; init; }
}
