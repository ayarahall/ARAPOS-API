using Arapos.Api.Contracts.Platform;
using Arapos.Api.Data;
using Arapos.Api.Data.Entities;
using Arapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;
[ApiController]
[Route("platform/tenants")]
[Authorize(Policy = AuthPolicies.PlatformOnly)]

public sealed class PlatformTenantsController : ControllerBase
{
    private readonly AraposDbContext _db;

    public PlatformTenantsController(AraposDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        var name = (req.Name ?? "").Trim();
        var slug = (req.Slug ?? "").Trim().ToLowerInvariant();

        if (name.Length < 2) return BadRequest("Name is required.");
        if (slug.Length < 2) return BadRequest("Slug is required.");

        var exists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Slug == slug, ct);
        if (exists) return Conflict("Slug already exists.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        return Ok(new { tenant.Id, tenant.Name, tenant.Slug, tenant.Status });
    }

    [HttpPost("{tenantId:guid}/branches")]
    public async Task<IActionResult> CreateBranch(
    [FromRoute] Guid tenantId,
    [FromBody] CreateBranchRequest req,
    CancellationToken ct)
    {
        var tenantExists = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId && t.Status == "ACTIVE", ct);

        if (!tenantExists)
            return NotFound("Tenant not found.");

        var code = (req.Code ?? "").Trim();
        var name = (req.Name ?? "").Trim();
        var currency = (req.CurrencyCode ?? "AED").Trim().ToUpperInvariant();

        if (code.Length < 1) return BadRequest("Code is required.");
        if (name.Length < 2) return BadRequest("Name is required.");
        if (currency.Length != 3) return BadRequest("CurrencyCode must be 3 letters.");

        var duplicate = await _db.Branches
     .IgnoreQueryFilters()
     .AsNoTracking()
     .AnyAsync(b => b.TenantId == tenantId && b.Code == code, ct);


        if (duplicate)
            return Conflict("Branch code already exists for this tenant.");

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            CurrencyCode = currency,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            branch.Id,
            branch.TenantId,
            branch.Code,
            branch.Name,
            branch.CurrencyCode,
            branch.IsActive
        });
    }


    [HttpPost("{tenantId:guid}/users")]
    public async Task<IActionResult> CreateTenantUser(
        [FromRoute] Guid tenantId,
        [FromBody] CreateTenantUserRequest req,
        CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId && t.Status == "ACTIVE", ct);
        if (tenant is null) return NotFound("Tenant not found.");

        var username = (req.Username ?? "").Trim();
        var role = (req.Role ?? "CASHIER").Trim().ToUpperInvariant();
        var pin = (req.Pin ?? "").Trim();

        if (username.Length < 3) return BadRequest("Username must be at least 3 chars.");
        if (role is not ("ADMIN" or "CASHIER")) return BadRequest("Role must be ADMIN or CASHIER.");
        if (pin.Length < 4) return BadRequest("PIN must be at least 4 digits.");

        var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Username == username, ct);
        if (userExists) return Conflict("Username already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = "PIN_ONLY",   // ✅ مهم: ما تكون null
            PinHash = null
        };


        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Set PIN for this user in this tenant using stored procedure.
        // Your DB has sp_SetUserPinV2 (confirmed).
        await _db.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_SetUserPinV2 @UserId={0}, @TenantId={1}, @Pin={2}",
            user.Id, tenantId, pin);

        return Ok(new { user.Id, user.Username, user.Role, TenantId = tenantId });
    }
}
