using Ayapos.Api.Contracts.Common;
using Ayapos.Api.Contracts.Services;
using Ayapos.Api.Data;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/services")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class ServicesController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public ServicesController(AyaposDbContext db, ITenantContext tenant)
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
            query = query.Where(x => (EF.Property<bool?>(x.s, nameof(Data.Entities.Service.IsActive)) ?? true));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                (x.s.NameAr != null && EF.Functions.Like(x.s.NameAr, $"%{term}%")) ||
                (x.s.NameEn != null && EF.Functions.Like(x.s.NameEn, $"%{term}%")));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => EF.Property<DateTime?>(x.s, nameof(Data.Entities.Service.CreatedAt)) ?? DateTime.UtcNow)
            .ThenBy(x => EF.Property<string?>(x.s, nameof(Data.Entities.Service.NameAr)) ?? string.Empty)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ServiceListItem
            {
                Id = x.s.Id,
                NameAr = x.s.NameAr,
                NameEn = x.s.NameEn,
                DurationMin = EF.Property<int?>(x.s, nameof(Data.Entities.Service.DurationMin)),
                IsActive = EF.Property<bool?>(x.s, nameof(Data.Entities.Service.IsActive)) ?? true,
                CreatedAt = EF.Property<DateTime?>(x.s, nameof(Data.Entities.Service.CreatedAt)) ?? DateTime.UtcNow,
                PriceCents = EF.Property<int?>(x.sp, nameof(Data.Entities.ServicePrice.PriceCents)) ?? 0,
                Price = (EF.Property<int?>(x.sp, nameof(Data.Entities.ServicePrice.PriceCents)) ?? 0) / 100m,
                CurrencyCode = EF.Property<string?>(x.sp, nameof(Data.Entities.ServicePrice.CurrencyCode)) ?? "AED"
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
                DurationMin = EF.Property<int?>(s, nameof(Data.Entities.Service.DurationMin)),
                IsActive = EF.Property<bool?>(s, nameof(Data.Entities.Service.IsActive)) ?? true,
                CreatedAt = EF.Property<DateTime?>(s, nameof(Data.Entities.Service.CreatedAt)) ?? DateTime.UtcNow,
                PriceCents = EF.Property<int?>(sp, nameof(Data.Entities.ServicePrice.PriceCents)) ?? 0,
                Price = (EF.Property<int?>(sp, nameof(Data.Entities.ServicePrice.PriceCents)) ?? 0) / 100m,
                CurrencyCode = EF.Property<string?>(sp, nameof(Data.Entities.ServicePrice.CurrencyCode)) ?? "AED"
            }
        ).SingleOrDefaultAsync(ct);

        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceListItem>> Create(
        [FromRoute] string tenantslug,
        [FromBody] CreateServiceRequest request,
        CancellationToken ct = default)
    {
        if (!CanManageCatalog())
            return Forbid();

        if (!TryGetTenantAndBranch(out var tenantId, out var branchId, out var err))
            return err!;

        var normalized = NormalizeRequest(
            request.NameAr,
            request.NameEn,
            request.DurationMin,
            request.Price,
            request.CurrencyCode,
            request.IsActive);

        if (normalized.Error is not null)
            return BadRequest(normalized.Error);

        var service = new Data.Entities.Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            NameAr = normalized.NameAr,
            NameEn = normalized.NameEn,
            DurationMin = normalized.DurationMin,
            IsActive = normalized.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var servicePrice = new Data.Entities.ServicePrice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            ServiceId = service.Id,
            PriceCents = DecimalToCents(normalized.Price),
            CurrencyCode = normalized.CurrencyCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Services.Add(service);
        _db.ServicePrices.Add(servicePrice);
        await _db.SaveChangesAsync(ct);

        return Ok(MapService(service, servicePrice));
    }

    [HttpPost("{id:guid}")]
    public async Task<ActionResult<ServiceListItem>> Update(
        [FromRoute] string tenantslug,
        [FromRoute] Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken ct = default)
    {
        if (!CanManageCatalog())
            return Forbid();

        if (!TryGetTenantAndBranch(out var tenantId, out var branchId, out var err))
            return err!;

        var service = await _db.Services.FirstOrDefaultAsync(
            item => item.Id == id
                && item.TenantId == tenantId
                && item.BranchId == branchId,
            ct);

        if (service is null)
            return NotFound("Service not found.");

        var servicePrice = await _db.ServicePrices.FirstOrDefaultAsync(
            item => item.ServiceId == id
                && item.TenantId == tenantId
                && item.BranchId == branchId,
            ct);

        if (servicePrice is null)
            return NotFound("Service price not found.");

        var normalized = NormalizeRequest(
            request.NameAr,
            request.NameEn,
            request.DurationMin,
            request.Price,
            request.CurrencyCode,
            request.IsActive);

        if (normalized.Error is not null)
            return BadRequest(normalized.Error);

        service.NameAr = normalized.NameAr;
        service.NameEn = normalized.NameEn;
        service.DurationMin = normalized.DurationMin;
        service.IsActive = normalized.IsActive;

        servicePrice.PriceCents = DecimalToCents(normalized.Price);
        servicePrice.CurrencyCode = normalized.CurrencyCode;
        servicePrice.IsActive = normalized.IsActive;

        await _db.SaveChangesAsync(ct);

        return Ok(MapService(service, servicePrice));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        [FromRoute] string tenantslug,
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!CanManageCatalog())
            return Forbid();

        if (!TryGetTenantAndBranch(out var tenantId, out var branchId, out var err))
            return err!;

        var service = await _db.Services.FirstOrDefaultAsync(
            s => s.Id == id && s.TenantId == tenantId && s.BranchId == branchId, ct);

        if (service is null) return NotFound("Service not found.");

        var prices = _db.ServicePrices.Where(sp => sp.ServiceId == id && sp.TenantId == tenantId && sp.BranchId == branchId);
        _db.ServicePrices.RemoveRange(prices);
        _db.Services.Remove(service);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ServiceListItem MapService(Data.Entities.Service service, Data.Entities.ServicePrice servicePrice)
        => new()
        {
            Id = service.Id,
            NameAr = service.NameAr,
            NameEn = service.NameEn,
            DurationMin = service.DurationMin,
            IsActive = service.IsActive,
            CreatedAt = service.CreatedAt,
            PriceCents = servicePrice.PriceCents,
            Price = servicePrice.PriceCents / 100m,
            CurrencyCode = servicePrice.CurrencyCode
        };

    private static int DecimalToCents(decimal amount)
        => (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static (
        string? NameAr,
        string? NameEn,
        int? DurationMin,
        decimal Price,
        string CurrencyCode,
        bool IsActive,
        string? Error) NormalizeRequest(
            string? nameAr,
            string? nameEn,
            int? durationMin,
            decimal price,
            string? currencyCode,
            bool isActive)
    {
        var normalizedNameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr.Trim();
        var normalizedNameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        var normalizedCurrency = string.IsNullOrWhiteSpace(currencyCode) ? "AED" : currencyCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedNameEn) && string.IsNullOrWhiteSpace(normalizedNameAr))
            return (null, null, durationMin, price, normalizedCurrency, isActive, "Service name is required.");

        if (price < 0)
            return (null, null, durationMin, price, normalizedCurrency, isActive, "Price cannot be negative.");

        if (normalizedCurrency.Length != 3)
            return (null, null, durationMin, price, normalizedCurrency, isActive, "Currency code must be 3 letters.");

        if (durationMin is not null && durationMin < 0)
            return (null, null, durationMin, price, normalizedCurrency, isActive, "Duration cannot be negative.");

        return (normalizedNameAr, normalizedNameEn, durationMin, price, normalizedCurrency, isActive, null);
    }

    private bool CanManageCatalog()
        => User.IsInRole("TENANT")
           || User.IsInRole("ADMIN")
           || User.IsInRole("OWNER");
}
