namespace Ayapos.EndUser.Models.Appointments;

public sealed class AppointmentResourceModel
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
