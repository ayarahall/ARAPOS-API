namespace Ayapos.Api.Contracts.Staff;

public sealed class EmployeeLeaveDto
{
    public Guid Id { get; init; }
    public Guid StaffId { get; init; }
    public string LeaveType { get; init; } = "";
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsPaid { get; init; }
    public string Status { get; init; } = "";
    public string? Notes { get; init; }
}
