namespace Ayapos.Api.Contracts.Appointments;

public sealed class AppointmentScheduleColumnDto
{
    public Guid? UserId { get; init; }
    public string ResourceName { get; init; } = "";
    public string Role { get; init; } = "";
    public bool IsUnassigned { get; init; }
    public List<AppointmentScheduleEntryDto> Items { get; init; } = [];
}
