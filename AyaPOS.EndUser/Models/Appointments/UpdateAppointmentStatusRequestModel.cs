namespace Ayapos.EndUser.Models.Appointments;

public sealed class UpdateAppointmentStatusRequestModel
{
    public string Status { get; set; } = "scheduled";
}
