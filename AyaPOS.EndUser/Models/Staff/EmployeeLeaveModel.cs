namespace Ayapos.EndUser.Models.Staff;

public sealed class EmployeeLeaveModel
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
