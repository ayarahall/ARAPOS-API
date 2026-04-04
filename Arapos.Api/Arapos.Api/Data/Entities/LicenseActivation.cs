using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class LicenseActivation
{
    public Guid Id { get; set; }

    public Guid LicenseId { get; set; }

    public string DeviceId { get; set; } = null!;

    public DateTime ActivatedAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public virtual License License { get; set; } = null!;
}
