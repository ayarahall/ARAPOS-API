namespace Ayapos.EndUser.Models.Staff;

public sealed class BranchAttendanceSummaryModel
{
    public DateTime Date { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int LeaveCount { get; set; }
    public decimal TotalDeductions { get; set; }
}
