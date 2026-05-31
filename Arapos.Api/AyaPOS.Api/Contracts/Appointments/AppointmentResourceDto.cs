namespace Ayapos.Api.Contracts.Appointments;

public sealed class AppointmentResourceDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string Role { get; init; } = "";
    public string? AppointmentColor { get; init; }
}


