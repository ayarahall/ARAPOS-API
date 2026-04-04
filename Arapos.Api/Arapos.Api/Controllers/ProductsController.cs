using Arapos.Api.Contracts.Common;
using Arapos.Api.Contracts.Products;
using Arapos.Api.Data;
using Arapos.Api.Security;
using Arapos.Api.Tenancy;
using Arapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/products")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class ProductsController : ControllerBase
{
    private readonly AraposDbContext _db;
    private readonly ITenantContext _tenant;

    public ProductsController(AraposDbContext db, ITenantContext tenant)
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
            query = query.Where(p => p.IsActive);

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
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.NameAr)
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
                SellPrice = p.SellPrice,
                CurrencyCode = p.CurrencyCode,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
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
                SellPrice = p.SellPrice,
                CurrencyCode = p.CurrencyCode,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .SingleOrDefaultAsync(ct);

        if (item is null) return NotFound();
        return Ok(item);
    }
}
