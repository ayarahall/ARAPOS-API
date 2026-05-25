using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class License
{
    public Guid Id { get; set; }

    public string LicenseKey { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public int? MaxDevices { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<LicenseActivation> LicenseActivations { get; set; } = new List<LicenseActivation>();
}
