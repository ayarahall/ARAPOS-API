using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class UserPinsHistory
{
    public Guid UserId { get; set; }

    public byte[] PinSalt { get; set; } = null!;

    public byte[] PinHash { get; set; } = null!;

    public string Algo { get; set; } = null!;

    public int Iterations { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime SysStartTime { get; set; }

    public DateTime SysEndTime { get; set; }

    public Guid TenantId { get; set; }
}
