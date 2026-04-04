using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class Staff
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? JobTitle { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
