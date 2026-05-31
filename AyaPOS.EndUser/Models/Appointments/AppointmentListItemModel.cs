namespace Ayapos.EndUser.Models.Appointments;

public sealed class AppointmentListItemModel
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ServiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal? ServicePrice { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public string ResourceName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
