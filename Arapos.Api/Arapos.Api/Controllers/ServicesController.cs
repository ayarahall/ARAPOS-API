using Arapos.Api.Contracts.Common;
using Arapos.Api.Contracts.Services;
using Arapos.Api.Data;
using Arapos.Api.Security;
using Arapos.Api.Tenancy;
using Arapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/services")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class ServicesController : ControllerBase
{
    private readonly AraposDbContext _db;
    private readonly ITenantContext _tenant;

    public ServicesController(AraposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private bool TryGetTenantAndBranch(out Guid tenantId, out Guid branchId, out ActionResult? error)
    {
        tenantId = default;
        branchId = default;
        error = null;

        if (_tenant.TenantId is null)
        {
            error = Unauthorized("Missing tenant context (TenantId).");
            return false;
        }

        if (_tenant.BranchId is null)
        {
            error = BadRequest("Missing branch context (X-Branch-Id).");
            return false;
        }

        tenantId = _tenant.TenantId.Value;
        branchId = _tenant.BranchId.Value;
        return true;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceListItem>>> List(
        [FromRoute] string tenantslug,
        [FromQuery] string? q,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!TryGetTenantAndBranch(out var tenantId, out var branchId, out var err))
            return err!;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        // Join Services + ServicePrices (active price per service per branch)
        var query =
            from s in _db.Services.AsNoTracking()
            join sp in _db.ServicePrices.AsNoTracking()
                on s.Id equals sp.ServiceId
            where s.TenantId == tenantId
               && s.BranchId == branchId
               && sp.TenantId == tenantId
               && sp.BranchId == branchId
               && sp.IsActive
            select new { s, sp };

        if (!includeInactive)
            query = query.Where(x => x.s.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                (x.s.NameAr != null && EF.Functions.Like(x.s.NameAr, $"%{term}%")) ||
                (x.s.NameEn != null && EF.Functions.Like(x.s.NameEn, $"%{term}%")));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.s.CreatedAt)
            .ThenBy(x => x.s.NameAr)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ServiceListItem
            {
                Id = x.s.Id,
                NameAr = x.s.NameAr,
                NameEn = x.s.NameEn,
                DurationMin = x.s.DurationMin,
                IsActive = x.s.IsActive,
                CreatedAt = x.s.CreatedAt,
                PriceCents = x.sp.PriceCents,
                Price = x.sp.PriceCents / 100m,
                CurrencyCode = x.sp.CurrencyCode
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<ServiceListItem>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceListItem>> GetById(
        [FromRoute] string tenantslug,
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!TryGetTenantAndBranch(out var tenantId, out var branchId, out var err))
            return err!;

        var item = await (
            from s in _db.Services.AsNoTracking()
            join sp in _db.ServicePrices.AsNoTracking()
                on s.Id equals sp.ServiceId
            where s.Id == id
               && s.TenantId == tenantId
               && s.BranchId == branchId
               && sp.TenantId == tenantId
               && sp.BranchId == branchId
               && sp.IsActive
            select new ServiceListItem
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                DurationMin = s.DurationMin,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                PriceCents = sp.PriceCents,
                Price = sp.PriceCents / 100m,
                CurrencyCode = sp.CurrencyCode
            }
        ).SingleOrDefaultAsync(ct);

        if (item is null)
            return NotFound();

        return Ok(item);
    }
}