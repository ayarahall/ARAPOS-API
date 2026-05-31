namespace Ayapos.EndUser.Models.Appointments;

public sealed class AppointmentScheduleBoardModel
{
    public DateTime Date { get; set; }
    public List<AppointmentScheduleColumnModel> Columns { get; set; } = [];
}
