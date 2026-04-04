using Arapos.Api.Contracts.Auth;
using Arapos.Api.Data;
using Arapos.Api.Data.Entities;
using Arapos.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AraposDbContext _db;
    private readonly JwtTokenService _tokens;

    public AuthController(AraposDbContext db, JwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("tenant/pin-login")]
    public async Task<ActionResult<LoginResponse>> TenantPinLogin([FromBody] TenantPinLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TenantSlug) ||
            string.IsNullOrWhiteSpace(req.Username) ||
            string.IsNullOrWhiteSpace(req.Pin))
            return BadRequest("tenantSlug, username, pin are required.");

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Slug == req.TenantSlug &&
                t.Status == "ACTIVE");

        if (tenant is null)
            return Unauthorized("Invalid tenant.");

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Username == req.Username &&
                u.IsActive);

        if (user is null)
            return Unauthorized("Invalid user.");

        // ensure user has pin row for this tenant
        var userPinExists = await _db.UserPins
         .IgnoreQueryFilters()
         .AsNoTracking()
         .AnyAsync(p => p.UserId == user.Id && p.TenantId == tenant.Id);


        if (!userPinExists)
            return Unauthorized("User not allowed for this tenant.");

        // Validate PIN via DB scalar function: dbo.fn_IsPinValid(@UserId,@TenantId,@Pin)
        // If your function name/signature differs, tell me and I'll adjust.


        var isValid = await _db.Database.SqlQueryRaw<int>(
        """
        SELECT CASE
          WHEN up.PinHash = HASHBYTES('SHA2_256', up.PinSalt + CONVERT(varbinary(max), {2}))
          THEN 1 ELSE 0 END AS Value
        FROM dbo.UserPins up
        WHERE up.UserId = {0} AND up.TenantId = {1}
        """,
        user.Id, tenant.Id, req.Pin)
    .SingleOrDefaultAsync();

        if (isValid != 1)
            return Unauthorized("Invalid PIN.");



        var role = string.IsNullOrWhiteSpace(user.Role) ? "CASHIER" : user.Role;
        var token = _tokens.CreateTenantToken(user.Id, tenant.Id, role);

        return Ok(new LoginResponse { Token = token });
    }


    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TenantSlug) ||
            string.IsNullOrWhiteSpace(req.Username) ||
            string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("tenantSlug, username, password required.");

        var tenant = await _db.Tenants
            .Include(t => t.License)
            .FirstOrDefaultAsync(t =>
                t.Slug == req.TenantSlug &&
                t.Status == "ACTIVE");

        if (tenant is null)
            return Unauthorized("Invalid tenant.");

        var license = tenant.License;

        if (license is null)
            return Forbid("No license assigned.");

        if (license.Status != "ACTIVE")
            return Forbid("License inactive.");

        if (license.ExpiresAt < DateTime.UtcNow)
            return Forbid("License expired.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Username == req.Username &&
                u.TenantId == tenant.Id &&
                u.IsActive);

        if (user is null)
            return Unauthorized("Invalid user.");

        // TODO: Replace with real password hash check
        if (user.PasswordHash != req.Password)
            return Unauthorized("Invalid password.");

        // 🔹 Device check
        var activeDevices = await _db.LicenseActivations
            .CountAsync(a => a.LicenseId == license.Id);

        if (activeDevices >= license.MaxDevices)
            return Forbid("Maximum devices reached.");

        _db.LicenseActivations.Add(new LicenseActivation
        {
            Id = Guid.NewGuid(),
            LicenseId = license.Id,
            DeviceId = Guid.NewGuid().ToString(), // replace later with real fingerprint
            ActivatedAt = DateTime.UtcNow
        });

        var role = string.IsNullOrWhiteSpace(user.Role) ? "CASHIER" : user.Role;

        var token = _tokens.CreateTenantToken(user.Id, tenant.Id, role);

        await _db.SaveChangesAsync();

        return Ok(new LoginResponse { Token = token });
    }


}
