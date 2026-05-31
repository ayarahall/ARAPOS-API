namespace Ayapos.EndUser.Models.Appointments;

public sealed class UpdateAppointmentRequestModel
{
    public Guid? CustomerId { get; set; }
    public Guid? ServiceId { get; set; }
    public DateTime StartAt { get; set; } = DateTime.Today.AddHours(10);
    public DateTime EndAt { get; set; } = DateTime.Today.AddHours(11);
    public string ResourceName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
