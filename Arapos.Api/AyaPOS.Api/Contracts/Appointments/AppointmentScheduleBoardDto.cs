namespace Ayapos.Api.Contracts.Appointments;

public sealed class AppointmentScheduleBoardDto
{
    public DateTime Date { get; init; }
    public List<AppointmentScheduleColumnDto> Columns { get; init; } = [];
}
