namespace Ayapos.Api.Contracts.Staff;

public sealed class CreateEmployeeLeaveRequest
{
    public string LeaveType { get; init; } = "";
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsPaid { get; init; }
    public string? Notes { get; init; }
}
