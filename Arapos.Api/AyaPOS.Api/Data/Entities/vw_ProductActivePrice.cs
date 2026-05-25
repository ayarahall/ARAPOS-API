using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class vw_ProductActivePrice
{
    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid ProductId { get; set; }

    public int PriceCents { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
