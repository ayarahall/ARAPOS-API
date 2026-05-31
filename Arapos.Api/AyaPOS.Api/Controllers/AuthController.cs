using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ayapos.Api.Contracts.Auth;
using Ayapos.Api.Contracts.Platform;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly JwtTokenService _tokens;
    private readonly PasswordHasherService _passwordHasher;

    public AuthController(
        AyaposDbContext db,
        JwtTokenService tokens,
        PasswordHasherService passwordHasher)
    {
        _db = db;
        _tokens = tokens;
        _passwordHasher = passwordHasher;
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
                t.Status == "ACTIVE" &&
                t.LicenseStatus == "ACTIVE" &&
                t.LicenseExpiresAt > DateTime.UtcNow);

        if (tenant is null)
            return Unauthorized("Invalid tenant.");

        var branchId = req.BranchId;
        if (branchId.HasValue)
        {
            var branchExists = await _db.Branches
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(b => b.TenantId == tenant.Id && b.Id == branchId.Value && b.IsActive);

            if (!branchExists)
                return Unauthorized("Invalid branch.");
        }

        User? user;
        if (branchId.HasValue)
        {
            user = await GetBranchUserForTenantAsync(tenant.Id, branchId.Value, req.Username)
                ?? await GetTenantAdminUserAsync(tenant.Id, req.Username);
        }
        else
        {
            user = await GetTenantAdminUserAsync(tenant.Id, req.Username);
        }

        if (user is null)
            return Unauthorized("Invalid user.");

        var pinRecord = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.TenantId == tenant.Id);

        if (pinRecord is null)
            return Unauthorized("Invalid PIN.");

        // Replicate SQL Server: SHA256(PinSalt + CONVERT(varbinary, pin))
        // CONVERT from nvarchar uses UTF-16 LE encoding
        var combined = pinRecord.PinSalt.Concat(Encoding.Unicode.GetBytes(req.Pin)).ToArray();
        var computed = SHA256.HashData(combined);
        if (!computed.SequenceEqual(pinRecord.PinHash))
            return Unauthorized("Invalid PIN.");

        var role = string.IsNullOrWhiteSpace(user.Role) ? "CASHIER" : user.Role;
        var token = _tokens.CreateTenantToken(user.Id, tenant.Id, role);
        var permissions = UserPermissionCatalog.GetEffectivePermissions(user);

        return Ok(new LoginResponse
        {
            Token = token,
            Role = role,
            TenantId = tenant.Id,
            Permissions = permissions,
            PermissionsConfigured = UserPermissionCatalog.HasExplicitPermissions(user)
        });
    }

    [AllowAnonymous]
    [HttpPost("platform/login")]
    public async Task<ActionResult<LoginResponse>> PlatformLogin([FromBody] PlatformLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("username and password are required.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Username == req.Username &&
                u.IsActive &&
                u.Role == "OWNER");

        if (user is null)
            return Unauthorized("Invalid owner account.");

        var passwordVerification = _passwordHasher.Verify(user.PasswordHash, req.Password);
        if (passwordVerification == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid password.");

        if (passwordVerification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.Hash(req.Password);
            await _db.SaveChangesAsync();
        }

        var token = _tokens.CreatePlatformToken(user.Id, "OWNER");
        return Ok(new LoginResponse
        {
            Token = token,
            Role = "OWNER",
            Permissions = UserPermissionCatalog.All,
            PermissionsConfigured = true
        });
    }

    [AllowAnonymous]
    [HttpGet("tenant/{tenantSlug}/branches")]
    public async Task<ActionResult<IReadOnlyList<TenantBranchOption>>> GetTenantBranches(
        [FromRoute] string tenantSlug,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            return BadRequest("tenantSlug is required.");

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Slug == tenantSlug &&
                t.Status == "ACTIVE" &&
                t.LicenseStatus == "ACTIVE" &&
                t.LicenseExpiresAt > DateTime.UtcNow, ct);

        if (tenant is null)
            return NotFound("Tenant not found or license is inactive.");

        var branches = await _db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.TenantId == tenant.Id && (EF.Property<bool?>(b, nameof(Branch.IsActive)) ?? true))
            .OrderBy(b => EF.Property<string?>(b, nameof(Branch.Name)) ?? string.Empty)
            .Select(b => new TenantBranchOption
            {
                Id = b.Id,
                Code = EF.Property<string?>(b, nameof(Branch.Code)) ?? string.Empty,
                Name = EF.Property<string?>(b, nameof(Branch.Name)) ?? string.Empty,
                CurrencyCode = EF.Property<string?>(b, nameof(Branch.CurrencyCode)) ?? "AED"
            })
            .ToListAsync(ct);

        return Ok(branches);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TenantSlug) ||
            string.IsNullOrWhiteSpace(req.Username) ||
            string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("tenantSlug, username, password required.");

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Slug == req.TenantSlug &&
                t.Status == "ACTIVE" &&
                t.LicenseStatus == "ACTIVE" &&
                t.LicenseExpiresAt > DateTime.UtcNow);

        if (tenant is null)
            return Unauthorized("Invalid tenant.");

        var branchId = req.BranchId;
        if (branchId.HasValue)
        {
            var branchExists = await _db.Branches
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(b => b.TenantId == tenant.Id && b.Id == branchId.Value && b.IsActive);

            if (!branchExists)
                return Unauthorized("Invalid branch.");
        }

        User? user;
        if (branchId.HasValue)
        {
            // Try branch user first, then fall back to tenant admin (TENANT role can log in from any branch)
            user = await GetBranchUserForTenantAsync(tenant.Id, branchId.Value, req.Username)
                ?? await GetTenantAdminUserAsync(tenant.Id, req.Username);
        }
        else
        {
            user = await GetTenantAdminUserAsync(tenant.Id, req.Username);
        }

        if (user is null)
            return Unauthorized("Invalid user.");

        var passwordVerification = _passwordHasher.Verify(user.PasswordHash, req.Password);
        if (passwordVerification == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid password.");

        if (passwordVerification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.Hash(req.Password);
            await _db.SaveChangesAsync();
        }

        var role = NormalizeTenantRole(user.Role);
        var token = _tokens.CreateTenantToken(user.Id, tenant.Id, role);
        var permissions = UserPermissionCatalog.GetEffectivePermissions(user);

        return Ok(new LoginResponse
        {
            Token = token,
            Role = role,
            TenantId = tenant.Id,
            Permissions = permissions,
            PermissionsConfigured = UserPermissionCatalog.HasExplicitPermissions(user)
        });
    }

    [Authorize(Policy = AuthPolicies.TenantOnly)]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest("currentPassword and newPassword are required.");

        if (req.NewPassword.Length < 6)
            return BadRequest("New password must be at least 6 characters.");

        var tenantIdClaim = User.FindFirstValue("tenantId");
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(tenantIdClaim, out var tenantId) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token context.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);

        if (user is null)
            return Unauthorized("User not found.");

        var userAllowedForTenant = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(p => p.UserId == user.Id && p.TenantId == tenantId, ct);

        if (!userAllowedForTenant)
            return Forbid("User is not assigned to this tenant.");

        var currentPasswordVerification = _passwordHasher.Verify(user.PasswordHash, req.CurrentPassword);
        if (currentPasswordVerification == PasswordVerificationResult.Failed)
            return Unauthorized("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.Hash(req.NewPassword);
        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = "Password updated successfully." });
    }

    private static string NormalizeTenantRole(string? role)
    {
        var normalized = (role ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "OWNER" => "OWNER",
            "TENANT" => "TENANT",
            "ADMIN" => "TENANT",
            "BRANCH_MANAGER" => "BRANCH_MANAGER",
            "HR" => "HR",
            "CASHIER" => "CASHIER",
            _ => "CASHIER"
        };
    }

    private Task<User?> GetTenantAdminUserAsync(Guid tenantId, string username)
    {
        return (
            from pin in _db.UserPins.IgnoreQueryFilters()
            join u in _db.Users on pin.UserId equals u.Id
            where pin.TenantId == tenantId &&
                  u.Username == username &&
                  (u.Role == "TENANT" || u.Role == "ADMIN") &&
                  u.IsActive &&
                  u.LicenseStatus == "ACTIVE" &&
                  u.LicenseExpiresAt > DateTime.UtcNow
            select u)
            .FirstOrDefaultAsync();
    }

    private Task<User?> GetBranchUserForTenantAsync(Guid tenantId, Guid branchId, string username)
    {
        return (
            from a in _db.BranchUserAssignments.IgnoreQueryFilters()
            join pin in _db.UserPins.IgnoreQueryFilters() on new { a.UserId, a.TenantId } equals new { pin.UserId, pin.TenantId }
            join u in _db.Users on a.UserId equals u.Id
            where a.TenantId == tenantId &&
                  a.BranchId == branchId &&
                  u.Username == username &&
                  u.IsActive &&
                  u.LicenseStatus == "ACTIVE" &&
                  u.LicenseExpiresAt > DateTime.UtcNow
            select u)
            .FirstOrDefaultAsync();
    }
}
