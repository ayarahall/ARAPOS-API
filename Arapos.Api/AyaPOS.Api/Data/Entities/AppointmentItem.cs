using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class AppointmentItem
{
    public Guid Id { get; set; }

    public Guid AppointmentId { get; set; }

    public string ItemType { get; set; } = null!;

    public Guid ItemId { get; set; }

    public string Name { get; set; } = null!;

    public int Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public virtual Appointment Appointment { get; set; } = null!;
}
