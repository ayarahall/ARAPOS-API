namespace Ayapos.EndUser.Models.Staff;

public sealed class EmployeeShiftModel
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GraceMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? WeeklyPattern { get; set; }
}
