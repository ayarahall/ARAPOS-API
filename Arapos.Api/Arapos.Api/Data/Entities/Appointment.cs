using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class Appointment
{
    public Guid Id { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? StaffId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AppointmentItem> AppointmentItems { get; set; } = new List<AppointmentItem>();

    public virtual User? CreatedByUser { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Staff? Staff { get; set; }
}
