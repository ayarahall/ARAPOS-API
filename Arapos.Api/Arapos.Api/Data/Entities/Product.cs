using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class Product
{
    public Guid Id { get; set; }

    public string? Sku { get; set; }

    public string? Barcode { get; set; }

    public string? NameAr { get; set; }

    public string? NameEn { get; set; }

    public string Unit { get; set; } = null!;

    public bool TrackInventory { get; set; }

    public int? ReorderPoint { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal SellPrice { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public decimal? CostPrice { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<InventoryMove> InventoryMoves { get; set; } = new List<InventoryMove>();

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ProductStockSnapshot? ProductStockSnapshot { get; set; }
}
