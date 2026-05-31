namespace Ayapos.EndUser.Models.Staff;

public sealed class CreateEmployeeLeaveRequestModel
{
    public string LeaveType { get; set; } = "annual";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public bool IsPaid { get; set; } = true;
    public string? Notes { get; set; }
}
