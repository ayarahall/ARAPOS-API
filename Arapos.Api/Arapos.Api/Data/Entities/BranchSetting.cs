using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class BranchSetting
{
    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public decimal MaxCashierDiscountPct { get; set; }

    public bool AllowLineDiscount { get; set; }

    public bool AllowInvoiceDiscount { get; set; }

    public bool RequireManagerForPriceOverride { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
