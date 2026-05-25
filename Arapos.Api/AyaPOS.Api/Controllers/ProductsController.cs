using Ayapos.Api.Contracts.Common;
using Ayapos.Api.Contracts.Products;
using Ayapos.Api.Data;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/products")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class ProductsController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public ProductsController(AyaposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItem>>> List(
        [FromRoute] string tenantslug,
        [FromQuery] string? q,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null) return Unauthorized("Missing tenant.");
        if (_tenant.BranchId is null) return BadRequest("X-Branch-Id is required.");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        var query = _db.Products.AsNoTracking()
            .Where(p => p.TenantId == _tenant.TenantId.Value && p.BranchId == _tenant.BranchId.Value);

        if (!includeInactive)
            query = query.Where(p => (EF.Property<bool?>(p, nameof(Data.Entities.Product.IsActive)) ?? true));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                (p.NameAr != null && EF.Functions.Like(p.NameAr, $"%{term}%")) ||
                (p.NameEn != null && EF.Functions.Like(p.NameEn, $"%{term}%")) ||
                (p.Sku != null && EF.Functions.Like(p.Sku, $"%{term}%")) ||
                (p.Barcode != null && EF.Functions.Like(p.Barcode, $"%{term}%")));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => EF.Property<DateTime?>(p, nameof(Data.Entities.Product.CreatedAt)) ?? DateTime.UtcNow)
            .ThenBy(p => EF.Property<string?>(p, nameof(Data.Entities.Product.NameAr)) ?? string.Empty)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItem
            {
                Id = p.Id,
                BranchId = p.BranchId,
                Sku = p.Sku,
                Barcode = p.Barcode,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                Unit = EF.Property<string?>(p, nameof(Data.Entities.Product.Unit)) ?? "pcs",
                SellPrice = EF.Property<decimal?>(p, nameof(Data.Entities.Product.SellPrice)) ?? 0m,
                CurrencyCode = EF.Property<string?>(p, nameof(Data.Entities.Product.CurrencyCode)) ?? "AED",
                IsActive = EF.Property<bool?>(p, nameof(Data.Entities.Product.IsActive)) ?? true,
                TrackInventory = EF.Property<bool?>(p, nameof(Data.Entities.Product.TrackInventory)) ?? true,
                CreatedAt = EF.Property<DateTime?>(p, nameof(Data.Entities.Product.CreatedAt)) ?? DateTime.UtcNow
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<ProductListItem>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductListItem>> GetById(
        [FromRoute] string tenantslug,
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null) return Unauthorized("Missing tenant.");
        if (_tenant.BranchId is null) return BadRequest("X-Branch-Id is required.");

        var item = await _db.Products.AsNoTracking()
            .Where(p =>
                p.Id == id &&
                p.TenantId == _tenant.TenantId.Value &&
                p.BranchId == _tenant.BranchId.Value)
            .Select(p => new ProductListItem
            {
                Id = p.Id,
                BranchId = p.BranchId,
                Sku = p.Sku,
                Barcode = p.Barcode,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                Unit = EF.Property<string?>(p, nameof(Data.Entities.Product.Unit)) ?? "pcs",
                SellPrice = EF.Property<decimal?>(p, nameof(Data.Entities.Product.SellPrice)) ?? 0m,
                CurrencyCode = EF.Property<string?>(p, nameof(Data.Entities.Product.CurrencyCode)) ?? "AED",
                IsActive = EF.Property<bool?>(p, nameof(Data.Entities.Product.IsActive)) ?? true,
                TrackInventory = EF.Property<bool?>(p, nameof(Data.Entities.Product.TrackInventory)) ?? true,
                CreatedAt = EF.Property<DateTime?>(p, nameof(Data.Entities.Product.CreatedAt)) ?? DateTime.UtcNow
            })
            .SingleOrDefaultAsync(ct);

        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ProductListItem>> Create(
        [FromRoute] string tenantslug,
        [FromBody] CreateProductRequest request,
        CancellationToken ct = default)
    {
        if (!CanManageCatalog())
            return Forbid();

        if (_tenant.TenantId is null) return Unauthorized("Missing tenant.");
        if (_tenant.BranchId is null) return BadRequest("X-Branch-Id is required.");

        var normalized = NormalizeRequest(
            request.Sku,
            request.Barcode,
            request.NameAr,
            request.NameEn,
            request.Unit,
            request.CurrencyCode,
            request.SellPrice,
            request.IsActive,
            request.TrackInventory);

        if (normalized.Error is not null)
            return BadRequest(normalized.Error);

        if (!string.IsNullOrWhiteSpace(normalized.Sku))
        {
            var skuExists = await _db.Products.AsNoTracking().AnyAsync(
                product => product.TenantId == _tenant.TenantId.Value
                    && product.BranchId == _tenant.BranchId.Value
                    && product.Sku == normalized.Sku,
                ct);

            if (skuExists)
                return Conflict("SKU already exists in this branch.");
        }

        if (!string.IsNullOrWhiteSpace(normalized.Barcode))
        {
            var barcodeExists = await _db.Products.AsNoTracking().AnyAsync(
                product => product.TenantId == _tenant.TenantId.Value
                    && product.BranchId == _tenant.BranchId.Value
                    && product.Barcode == normalized.Barcode,
                ct);

            if (barcodeExists)
                return Conflict("Barcode already exists in this branch.");
        }

        var product = new Data.Entities.Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            BranchId = _tenant.BranchId.Value,
            Sku = normalized.Sku,
            Barcode = normalized.Barcode,
            NameAr = normalized.NameAr,
            NameEn = normalized.NameEn,
            Unit = normalized.Unit,
            SellPrice = normalized.SellPrice,
            CurrencyCode = normalized.CurrencyCode,
            IsActive = normalized.IsActive,
            TrackInventory = normalized.TrackInventory,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        return Ok(MapProduct(product));
    }

    [HttpPost("{id:guid}")]
    public async Task<ActionResult<ProductListItem>> Update(
        [FromRoute] string tenantslug,
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct = default)
    {
        if (!CanManageCatalog())
            return Forbid();

        if (_tenant.TenantId is null) return Unauthorized("Missing tenant.");
        if (_tenant.BranchId is null) return BadRequest("X-Branch-Id is required.");

        var product = await _db.Products.FirstOrDefaultAsync(
            item => item.Id == id
                && item.TenantId == _tenant.TenantId.Value
                && item.BranchId == _tenant.BranchId.Value,
            ct);

        if (product is null)
            return NotFound("Product not found.");

        var normalized = NormalizeRequest(
            request.Sku,
            request.Barcode,
            request.NameAr,
            request.NameEn,
            request.Unit,
            request.CurrencyCode,
            request.SellPrice,
            request.IsActive,
            request.TrackInventory);

        if (normalized.Error is not null)
            return BadRequest(normalized.Error);

        if (!string.IsNullOrWhiteSpace(normalized.Sku))
        {
            var skuExists = await _db.Products.AsNoTracking().AnyAsync(
                item => item.TenantId == _tenant.TenantId.Value
                    && item.BranchId == _tenant.BranchId.Value
                    && item.Sku == normalized.Sku
                    && item.Id != id,
                ct);

            if (skuExists)
                return Conflict("SKU already exists in this branch.");
        }

        if (!string.IsNullOrWhiteSpace(normalized.Barcode))
        {
            var barcodeExists = await _db.Products.AsNoTracking().AnyAsync(
                item => item.TenantId == _tenant.TenantId.Value
                    && item.BranchId == _tenant.BranchId.Value
                    && item.Barcode == normalized.Barcode
                    && item.Id != id,
                ct);

            if (barcodeExists)
                return Conflict("Barcode already exists in this branch.");
        }

        product.Sku = normalized.Sku;
        product.Barcode = normalized.Barcode;
        product.NameAr = normalized.NameAr;
        product.NameEn = normalized.NameEn;
        product.Unit = normalized.Unit;
        product.SellPrice = normalized.SellPrice;
        product.CurrencyCode = normalized.CurrencyCode;
        product.IsActive = normalized.IsActive;
        product.TrackInventory = normalized.TrackInventory;

        await _db.SaveChangesAsync(ct);

        return Ok(MapProduct(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        [FromRoute] string tenantslug,
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!CanManageCatalog())
            return Forbid();

        if (_tenant.TenantId is null) return Unauthorized("Missing tenant.");
        if (_tenant.BranchId is null) return BadRequest("X-Branch-Id is required.");

        var product = await _db.Products.FirstOrDefaultAsync(
            p => p.Id == id && p.TenantId == _tenant.TenantId.Value && p.BranchId == _tenant.BranchId.Value, ct);

        if (product is null) return NotFound("Product not found.");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ProductListItem MapProduct(Data.Entities.Product product)
        => new()
        {
            Id = product.Id,
            BranchId = product.BranchId,
            Sku = product.Sku,
            Barcode = product.Barcode,
            NameAr = product.NameAr,
            NameEn = product.NameEn,
            Unit = product.Unit,
            SellPrice = product.SellPrice,
            CurrencyCode = product.CurrencyCode,
            IsActive = product.IsActive,
            TrackInventory = product.TrackInventory,
            CreatedAt = product.CreatedAt
        };

    private static (
        string? Sku,
        string? Barcode,
        string? NameAr,
        string? NameEn,
        string Unit,
        string CurrencyCode,
        decimal SellPrice,
        bool IsActive,
        bool TrackInventory,
        string? Error) NormalizeRequest(
            string? sku,
            string? barcode,
            string? nameAr,
            string? nameEn,
            string? unit,
            string? currencyCode,
            decimal sellPrice,
            bool isActive,
            bool trackInventory)
    {
        var normalizedSku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
        var normalizedBarcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        var normalizedNameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr.Trim();
        var normalizedNameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        var normalizedUnit = string.IsNullOrWhiteSpace(unit) ? "pcs" : unit.Trim().ToLowerInvariant();
        var normalizedCurrency = string.IsNullOrWhiteSpace(currencyCode) ? "AED" : currencyCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedNameEn) && string.IsNullOrWhiteSpace(normalizedNameAr))
            return (null, null, null, null, normalizedUnit, normalizedCurrency, sellPrice, isActive, trackInventory, "Product name is required.");

        if (sellPrice < 0)
            return (null, null, null, null, normalizedUnit, normalizedCurrency, sellPrice, isActive, trackInventory, "Sell price cannot be negative.");

        if (normalizedCurrency.Length != 3)
            return (null, null, null, null, normalizedUnit, normalizedCurrency, sellPrice, isActive, trackInventory, "Currency code must be 3 letters.");

        if (normalizedUnit.Length > 20)
            return (null, null, null, null, normalizedUnit, normalizedCurrency, sellPrice, isActive, trackInventory, "Unit must be 20 characters or fewer.");

        return (normalizedSku, normalizedBarcode, normalizedNameAr, normalizedNameEn, normalizedUnit, normalizedCurrency, sellPrice, isActive, trackInventory, null);
    }

    private bool CanManageCatalog()
        => User.IsInRole("TENANT")
           || User.IsInRole("ADMIN")
           || User.IsInRole("OWNER");
}
