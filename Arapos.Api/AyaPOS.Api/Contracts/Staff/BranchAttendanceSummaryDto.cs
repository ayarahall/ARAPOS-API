namespace Ayapos.Api.Contracts.Staff;

public sealed class BranchAttendanceSummaryDto
{
    public DateTime Date { get; init; }
    public int TotalEmployees { get; init; }
    public int PresentCount { get; init; }
    public int LateCount { get; init; }
    public int AbsentCount { get; init; }
    public int LeaveCount { get; init; }
    public decimal TotalDeductions { get; init; }
}
