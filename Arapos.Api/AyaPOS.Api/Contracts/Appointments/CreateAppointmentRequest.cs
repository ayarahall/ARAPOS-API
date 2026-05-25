namespace Ayapos.Api.Contracts.Appointments;

public sealed class CreateAppointmentRequest
{
    public Guid? CustomerId { get; init; }
    public Guid? ServiceId { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string? ResourceName { get; init; }
    public string? Notes { get; init; }
}
