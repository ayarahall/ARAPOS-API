namespace Ayapos.EndUser.Models.Appointments;

public sealed class AppointmentScheduleColumnModel
{
    public Guid? UserId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsUnassigned { get; set; }
    public List<AppointmentScheduleEntryModel> Items { get; set; } = [];
}
