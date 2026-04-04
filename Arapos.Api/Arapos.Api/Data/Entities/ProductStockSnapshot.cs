using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class ProductStockSnapshot
{
    public Guid ProductId { get; set; }

    public int QtyOnHand { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public virtual Product Product { get; set; } = null!;
}
