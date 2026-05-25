namespace Ayapos.Api.Contracts.Staff;

public sealed class EmployeeShiftDto
{
    public Guid Id { get; init; }
    public Guid StaffId { get; init; }
    public string Name { get; init; } = "";
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public int GraceMinutes { get; init; }
    public bool IsActive { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? WeeklyPattern { get; init; }
}
