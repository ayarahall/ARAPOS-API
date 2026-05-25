namespace Ayapos.Api.Contracts.Appointments;

public sealed class AppointmentListItemDto
{
    public Guid Id { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? ServiceId { get; init; }
    public string CustomerName { get; init; } = "";
    public string CustomerPhone { get; init; } = "";
    public string ServiceName { get; init; } = "";
    public decimal? ServicePrice { get; init; }
    public string CurrencyCode { get; init; } = "AED";
    public string ResourceName { get; set; } = "";
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string Status { get; init; } = "";
    public string Notes { get; set; } = "";
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
