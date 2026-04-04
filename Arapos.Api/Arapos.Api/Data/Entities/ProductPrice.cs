using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class ProductPrice
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid ProductId { get; set; }

    public int PriceCents { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
