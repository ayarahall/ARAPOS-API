using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class InventoryMove
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string MoveType { get; set; } = null!;

    public int Qty { get; set; }

    public string? Reason { get; set; }

    public string? RefType { get; set; }

    public Guid? RefId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual Product Product { get; set; } = null!;
}
