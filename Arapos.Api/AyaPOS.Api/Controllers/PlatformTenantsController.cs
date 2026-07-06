using System.Security.Cryptography;
using System.Text;
using Ayapos.Api.Contracts.Platform;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;
[ApiController]
[Route("platform/tenants")]
[Authorize(Policy = AuthPolicies.PlatformOnly)]

public sealed class PlatformTenantsController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly PasswordHasherService _passwordHasher;

    public PlatformTenantsController(AyaposDbContext db, PasswordHasherService passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlatformTenantSummaryDto>>> ListTenants(CancellationToken ct)
    {
        var tenantUserCounts = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .GroupBy(up => up.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                Count = g.Select(x => x.UserId).Distinct().Count()
            })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var items = await _db.Tenants
            .AsNoTracking()
            .OrderByDescending(tenant => tenant.CreatedAt)
            .Select(tenant => new PlatformTenantSummaryDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Slug,
                Status = tenant.Status,
                LicensePlan = tenant.LicensePlan,
                LicenseStatus = tenant.LicenseStatus,
                MaxUsers = tenant.MaxUsers,
                AssignedUsers = 0,
                LicenseStartedAt = tenant.LicenseStartedAt,
                LicenseExpiresAt = tenant.LicenseExpiresAt
            })
            .ToListAsync(ct);

        foreach (var item in items)
        {
            if (tenantUserCounts.TryGetValue(item.Id, out var assignedUsers))
            {
                item.AssignedUsers = assignedUsers;
            }
        }

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        var name = (req.Name ?? "").Trim();
        var slug = (req.Slug ?? "").Trim().ToLowerInvariant();
        var licensePlan = NormalizeLicensePlan(req.LicensePlan);
        var maxUsers = req.MaxUsers < 1 ? 1 : req.MaxUsers;

        if (name.Length < 2) return BadRequest("Name is required.");
        if (slug.Length < 2) return BadRequest("Slug is required.");
        if (licensePlan is null) return BadRequest("LicensePlan must be MONTHLY or YEARLY.");

        var exists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Slug == slug, ct);
        if (exists) return Conflict("Slug already exists.");

        var licenseStartedAt = DateTime.UtcNow;
        var licenseExpiresAt = CalculateLicenseExpiry(licenseStartedAt, licensePlan);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Status = "ACTIVE",
            LicensePlan = licensePlan,
            LicenseStatus = "ACTIVE",
            MaxUsers = maxUsers,
            LicenseStartedAt = licenseStartedAt,
            LicenseExpiresAt = licenseExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Status,
            tenant.LicensePlan,
            tenant.LicenseStatus,
            tenant.MaxUsers,
            tenant.LicenseStartedAt,
            tenant.LicenseExpiresAt
        });
    }

    [HttpPost("{tenantId:guid}/status")]
    public async Task<IActionResult> UpdateTenantStatus(
        [FromRoute] Guid tenantId,
        [FromBody] UpdateTenantStatusRequest req,
        CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return NotFound("Tenant not found.");

        tenant.Status = req.IsActive ? "ACTIVE" : "INACTIVE";
        await _db.SaveChangesAsync(ct);

        return Ok(new { tenant.Id, tenant.Name, tenant.Status });
    }

    [HttpPost("{tenantId:guid}/license")]
    public async Task<IActionResult> UpdateTenantLicense(
        [FromRoute] Guid tenantId,
        [FromBody] UpdateTenantLicenseRequest req,
        CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return NotFound("Tenant not found.");

        var licensePlan = NormalizeLicensePlan(req.LicensePlan);
        if (licensePlan is null)
            return BadRequest("LicensePlan must be MONTHLY or YEARLY.");

        var licenseStartedAt = req.LicenseStartedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        var maxUsers = req.MaxUsers < 1 ? 1 : req.MaxUsers;

        tenant.LicensePlan = licensePlan;
        tenant.LicenseStatus = "ACTIVE";
        tenant.MaxUsers = maxUsers;
        tenant.LicenseStartedAt = licenseStartedAt;
        tenant.LicenseExpiresAt = req.LicenseExpiresAt?.ToUniversalTime()
            ?? CalculateLicenseExpiry(licenseStartedAt, licensePlan);

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.LicensePlan,
            tenant.LicenseStatus,
            tenant.MaxUsers,
            tenant.LicenseStartedAt,
            tenant.LicenseExpiresAt
        });
    }

    [HttpGet("{tenantId:guid}/branches")]
    public async Task<ActionResult<IReadOnlyList<PlatformBranchDto>>> ListBranches(
        [FromRoute] Guid tenantId,
        CancellationToken ct)
    {
        var tenantExists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId, ct);
        if (!tenantExists)
            return NotFound("Tenant not found.");

        // SQL-Server bracket syntax ([Branches], [IsActive]) is not valid Postgres —
        // it's a parse error on every call, not just a no-op. Double-quoted identifiers
        // work on both providers' actual production target (Postgres).
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Branches\" SET \"IsActive\" = true WHERE \"TenantId\" = {0} AND \"IsActive\" IS NULL",
            tenantId);

        var items = await _db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(branch => branch.TenantId == tenantId)
            .OrderBy(branch => EF.Property<string?>(branch, nameof(Branch.Name)) ?? string.Empty)
            .Select(branch => new PlatformBranchDto
            {
                Id = branch.Id,
                TenantId = branch.TenantId,
                Name = EF.Property<string?>(branch, nameof(Branch.Name)) ?? string.Empty,
                Code = EF.Property<string?>(branch, nameof(Branch.Code)) ?? string.Empty,
                CurrencyCode = EF.Property<string?>(branch, nameof(Branch.CurrencyCode)) ?? "AED",
                IsActive = EF.Property<bool?>(branch, nameof(Branch.IsActive)) ?? true,
                AssignedUsers = _db.BranchUserAssignments
                    .AsNoTracking()
                    .Where(assignment => assignment.TenantId == branch.TenantId && assignment.BranchId == branch.Id)
                    .Select(assignment => assignment.UserId)
                    .Distinct()
                    .Count()
            })
            .ToListAsync(ct);

        return Ok(items);
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

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Branches\" SET \"IsActive\" = true WHERE \"Id\" = {0} AND \"IsActive\" IS NULL",
            branch.Id);
        branch.IsActive = true;

        return Ok(new PlatformBranchDto
        {
            Id = branch.Id,
            TenantId = branch.TenantId,
            Code = branch.Code,
            Name = branch.Name,
            CurrencyCode = branch.CurrencyCode,
            IsActive = branch.IsActive,
            AssignedUsers = 0
        });
    }

    [HttpPost("{tenantId:guid}/branches/{branchId:guid}")]
    public async Task<IActionResult> UpdateBranch(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid branchId,
        [FromBody] UpdateBranchRequest req,
        CancellationToken ct)
    {
        var tenantExists = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId && t.Status == "ACTIVE", ct);

        if (!tenantExists)
            return NotFound("Tenant not found.");

        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.TenantId == tenantId, ct);
        if (branch is null)
            return NotFound("Branch not found.");

        var code = (req.Code ?? "").Trim();
        var name = (req.Name ?? "").Trim();
        var currency = (req.CurrencyCode ?? "AED").Trim().ToUpperInvariant();

        if (code.Length < 1) return BadRequest("Code is required.");
        if (name.Length < 2) return BadRequest("Name is required.");
        if (currency.Length != 3) return BadRequest("CurrencyCode must be 3 letters.");

        var duplicate = await _db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(b => b.TenantId == tenantId && b.Code == code && b.Id != branchId, ct);

        if (duplicate)
            return Conflict("Branch code already exists for this tenant.");

        branch.Code = code;
        branch.Name = name;
        branch.CurrencyCode = currency;
        branch.IsActive = req.IsActive;

        await _db.SaveChangesAsync(ct);

        var assignedUsers = await _db.BranchUserAssignments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.BranchId == branchId)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync(ct);

        return Ok(new PlatformBranchDto
        {
            Id = branch.Id,
            TenantId = branch.TenantId,
            Name = branch.Name,
            Code = branch.Code,
            CurrencyCode = branch.CurrencyCode,
            IsActive = branch.IsActive,
            AssignedUsers = assignedUsers
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
        var role = NormalizeTenantRole(req.Role);
        var password = req.Password?.Trim();
        var pin = (req.Pin ?? "").Trim();
        var licensePlan = NormalizeLicensePlan(req.LicensePlan);

        if (username.Length < 3) return BadRequest("Username must be at least 3 chars.");
        if (role is null) return BadRequest("Role must be TENANT or CASHIER.");
        if (pin.Length < 4) return BadRequest("PIN must be at least 4 digits.");
        if (licensePlan is null) return BadRequest("LicensePlan must be MONTHLY or YEARLY.");

        var userExists = await UsernameExistsInTenantAsync(tenantId, username, ct);
        if (userExists) return Conflict("Username already exists for this tenant.");

        var assignedUsersCount = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .Select(p => p.UserId)
            .Distinct()
            .CountAsync(ct);

        if (assignedUsersCount >= tenant.MaxUsers)
            return BadRequest($"Tenant has reached its maximum allowed users ({tenant.MaxUsers}).");

        var licenseStartedAt = DateTime.UtcNow;
        var licenseExpiresAt = CalculateLicenseExpiry(licenseStartedAt, licensePlan);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Role = role,
            IsActive = true,
            LicensePlan = licensePlan,
            LicenseStatus = "ACTIVE",
            LicenseStartedAt = licenseStartedAt,
            LicenseExpiresAt = licenseExpiresAt,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = string.IsNullOrWhiteSpace(password)
                ? "PIN_ONLY"
                : _passwordHasher.Hash(password),
            PinHash = null
        };


        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Set the PIN for this user in this tenant. Previously this shelled out to a
        // SQL-Server-only stored proc (sp_SetUserPinV2) via EXEC — that syntax doesn't
        // exist on Postgres (production's actual provider) and threw a 500 on every
        // call. Salted SHA-256 UserPins insert instead, matching the working pattern
        // already used in TenantAdminController's CreateBranchUser.
        var pinSalt = RandomNumberGenerator.GetBytes(32);
        var pinHash = SHA256.HashData(pinSalt.Concat(Encoding.Unicode.GetBytes(pin)).ToArray());
        _db.UserPins.Add(new UserPin
        {
            UserId = user.Id,
            TenantId = tenantId,
            PinSalt = pinSalt,
            PinHash = pinHash,
            Algo = "SHA2_256",
            Iterations = 1,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Role,
            user.LicensePlan,
            user.LicenseStatus,
            user.LicenseStartedAt,
            user.LicenseExpiresAt,
            TenantId = tenantId
        });
    }

    [HttpGet("{tenantId:guid}/users")]
    public async Task<ActionResult<IReadOnlyList<PlatformTenantUserDto>>> ListTenantUsers(
        [FromRoute] Guid tenantId,
        CancellationToken ct)
    {
        var tenantExists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId, ct);
        if (!tenantExists)
            return NotFound("Tenant not found.");

        var items = await (
            from up in _db.UserPins.IgnoreQueryFilters().AsNoTracking()
            join user in _db.Users.AsNoTracking() on up.UserId equals user.Id
            where up.TenantId == tenantId
            orderby user.CreatedAt descending
            select new PlatformTenantUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive,
                LicensePlan = user.LicensePlan,
                LicenseStatus = user.LicenseStatus,
                LicenseStartedAt = user.LicenseStartedAt,
                LicenseExpiresAt = user.LicenseExpiresAt,
                CreatedAt = user.CreatedAt
            })
            .Distinct()
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{tenantId:guid}/branches/{branchId:guid}/users")]
    public async Task<ActionResult<IReadOnlyList<PlatformBranchUserDto>>> ListBranchUsers(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid branchId,
        CancellationToken ct)
    {
        var branchExists = await BranchExistsAsync(tenantId, branchId, requireActive: false, ct);

        if (!branchExists)
            return NotFound("Branch not found.");

        // Users explicitly assigned to a DIFFERENT branch of this same tenant.
        // These should not bleed into this branch's list.
        var usersInOtherBranches = _db.BranchUserAssignments
            .AsNoTracking()
            .Where(bua => bua.TenantId == tenantId && bua.BranchId != branchId)
            .Select(bua => bua.UserId);

        // Return: all users belonging to this tenant (via UserPins) who are NOT
        // exclusively assigned to a different branch.
        var items = await (
            from up in _db.UserPins.IgnoreQueryFilters().AsNoTracking()
            join user in _db.Users.AsNoTracking() on up.UserId equals user.Id
            where up.TenantId == tenantId
               && !usersInOtherBranches.Contains(user.Id)
            select new PlatformBranchUserDto
            {
                Id = user.Id,
                BranchId = branchId,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive,
                LicensePlan = user.LicensePlan,
                LicenseStatus = user.LicenseStatus,
                LicenseStartedAt = user.LicenseStartedAt,
                LicenseExpiresAt = user.LicenseExpiresAt,
                CreatedAt = user.CreatedAt
            })
            .Distinct()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("{tenantId:guid}/branches/{branchId:guid}/users")]
    public async Task<IActionResult> CreateBranchUser(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid branchId,
        [FromBody] CreateBranchUserRequest req,
        CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId && t.Status == "ACTIVE", ct);
        if (tenant is null) return NotFound("Tenant not found.");

        var branchExists = await BranchExistsAsync(tenantId, branchId, requireActive: true, ct);
        if (!branchExists) return NotFound("Branch not found.");

        var username = (req.Username ?? "").Trim();
        var role = NormalizeBranchRole(req.Role);
        var password = req.Password?.Trim();
        var pin = (req.Pin ?? "").Trim();
        var licensePlan = NormalizeLicensePlan(req.LicensePlan);

        if (username.Length < 3) return BadRequest("Username must be at least 3 chars.");
        if (role is null) return BadRequest("Role must be BRANCH_MANAGER, HR, or CASHIER.");
        if (pin.Length < 4) return BadRequest("PIN must be at least 4 digits.");
        if (licensePlan is null) return BadRequest("LicensePlan must be MONTHLY or YEARLY.");

        var userExists = await UsernameExistsInBranchAsync(tenantId, branchId, username, ct);
        if (userExists) return Conflict("Username already exists for this branch.");

        var assignedUsersCount = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .Select(p => p.UserId)
            .Distinct()
            .CountAsync(ct);

        if (assignedUsersCount >= tenant.MaxUsers)
            return BadRequest($"Tenant has reached its maximum allowed users ({tenant.MaxUsers}).");

        var licenseStartedAt = DateTime.UtcNow;
        var licenseExpiresAt = CalculateLicenseExpiry(licenseStartedAt, licensePlan);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Role = role,
            IsActive = true,
            LicensePlan = licensePlan,
            LicenseStatus = "ACTIVE",
            LicenseStartedAt = licenseStartedAt,
            LicenseExpiresAt = licenseExpiresAt,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = string.IsNullOrWhiteSpace(password)
                ? "PIN_ONLY"
                : _passwordHasher.Hash(password),
            PinHash = null
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Same case-folding problem as the pin insert below — unquoted raw SQL against
        // Postgres looks for a lowercase "branchuserassignments" table that doesn't
        // exist. Plain EF Core insert sidesteps it entirely.
        _db.BranchUserAssignments.Add(new BranchUserAssignment
        {
            UserId = user.Id,
            TenantId = tenantId,
            BranchId = branchId,
            AssignedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        var pinSalt = RandomNumberGenerator.GetBytes(32);
        var pinHash = SHA256.HashData(pinSalt.Concat(Encoding.Unicode.GetBytes(pin)).ToArray());
        _db.UserPins.Add(new UserPin
        {
            UserId = user.Id,
            TenantId = tenantId,
            PinSalt = pinSalt,
            PinHash = pinHash,
            Algo = "SHA2_256",
            Iterations = 1,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Ok(new PlatformBranchUserDto
        {
            Id = user.Id,
            BranchId = branchId,
            Username = user.Username,
            Role = user.Role,
            IsActive = user.IsActive,
            LicensePlan = user.LicensePlan,
            LicenseStatus = user.LicenseStatus,
            LicenseStartedAt = user.LicenseStartedAt,
            LicenseExpiresAt = user.LicenseExpiresAt,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("{tenantId:guid}/branches/{branchId:guid}/users/{userId:guid}/license")]
    public async Task<IActionResult> UpdateBranchUserLicense(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid userId,
        [FromBody] UpdateTenantUserLicenseRequest req,
        CancellationToken ct)
    {
        var branchAssignmentExists = await _db.BranchUserAssignments
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId && x.BranchId == branchId, ct);

        if (!branchAssignmentExists)
            return NotFound("Branch user not found.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return NotFound("User not found.");

        var licensePlan = NormalizeLicensePlan(req.LicensePlan);
        if (licensePlan is null)
            return BadRequest("LicensePlan must be MONTHLY or YEARLY.");

        var licenseStartedAt = req.LicenseStartedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        user.IsActive = req.IsActive;
        user.LicensePlan = licensePlan;
        user.LicenseStatus = req.IsActive ? "ACTIVE" : "INACTIVE";
        user.LicenseStartedAt = licenseStartedAt;
        user.LicenseExpiresAt = CalculateLicenseExpiry(licenseStartedAt, licensePlan);

        await _db.SaveChangesAsync(ct);

        return Ok(new PlatformBranchUserDto
        {
            Id = user.Id,
            BranchId = branchId,
            Username = user.Username,
            Role = user.Role,
            IsActive = user.IsActive,
            LicensePlan = user.LicensePlan,
            LicenseStatus = user.LicenseStatus,
            LicenseStartedAt = user.LicenseStartedAt,
            LicenseExpiresAt = user.LicenseExpiresAt,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("{tenantId:guid}/users/{userId:guid}/license")]
    public async Task<IActionResult> UpdateTenantUserLicense(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid userId,
        [FromBody] UpdateTenantUserLicenseRequest req,
        CancellationToken ct)
    {
        var tenantExists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId, ct);
        if (!tenantExists)
            return NotFound("Tenant not found.");

        var userAssignedToTenant = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(p => p.UserId == userId && p.TenantId == tenantId, ct);

        if (!userAssignedToTenant)
            return NotFound("User not found for this tenant.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return NotFound("User not found.");

        var licensePlan = NormalizeLicensePlan(req.LicensePlan);
        if (licensePlan is null)
            return BadRequest("LicensePlan must be MONTHLY or YEARLY.");

        var licenseStartedAt = req.LicenseStartedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        user.IsActive = req.IsActive;
        user.LicensePlan = licensePlan;
        user.LicenseStatus = req.IsActive ? "ACTIVE" : "INACTIVE";
        user.LicenseStartedAt = licenseStartedAt;
        user.LicenseExpiresAt = CalculateLicenseExpiry(licenseStartedAt, licensePlan);

        await _db.SaveChangesAsync(ct);

        return Ok(new PlatformTenantUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            IsActive = user.IsActive,
            LicensePlan = user.LicensePlan,
            LicenseStatus = user.LicenseStatus,
            LicenseStartedAt = user.LicenseStartedAt,
            LicenseExpiresAt = user.LicenseExpiresAt,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("{tenantId:guid}/users/{userId:guid}/password")]
    public async Task<IActionResult> SetTenantUserPassword(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid userId,
        [FromBody] SetTenantUserPasswordRequest req,
        CancellationToken ct)
    {
        var newPassword = (req.NewPassword ?? "").Trim();
        if (newPassword.Length < 6)
            return BadRequest("New password must be at least 6 characters.");

        var tenantExists = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId && t.Status == "ACTIVE", ct);

        if (!tenantExists)
            return NotFound("Tenant not found.");

        var userAssignedToTenant = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(p => p.UserId == userId && p.TenantId == tenantId, ct);

        if (!userAssignedToTenant)
            return NotFound("User not found for this tenant.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user is null)
            return NotFound("User not found.");

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = "Password set successfully.", user.Id, TenantId = tenantId });
    }

    [HttpPost("{tenantId:guid}/branches/{branchId:guid}/users/{userId:guid}/password")]
    public async Task<IActionResult> SetBranchUserPassword(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid userId,
        [FromBody] SetTenantUserPasswordRequest req,
        CancellationToken ct)
    {
        var newPassword = (req.NewPassword ?? "").Trim();
        if (newPassword.Length < 6)
            return BadRequest("New password must be at least 6 characters.");

        var branchAssignmentExists = await _db.BranchUserAssignments
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId && x.BranchId == branchId, ct);

        if (!branchAssignmentExists)
            return NotFound("Branch user not found.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user is null)
            return NotFound("User not found.");

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = "Password set successfully.", user.Id, TenantId = tenantId, BranchId = branchId });
    }

    private static string? NormalizeLicensePlan(string? licensePlan)
    {
        var normalized = (licensePlan ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "MONTHLY" => "MONTHLY",
            "YEARLY" => "YEARLY",
            "ANNUAL" => "YEARLY",
            _ => null
        };
    }

    private static string? NormalizeTenantRole(string? role)
    {
        var normalized = (role ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "TENANT" => "TENANT",
            "ADMIN" => "TENANT",
            "CASHIER" => "CASHIER",
            _ => null
        };
    }

    private static string? NormalizeBranchRole(string? role)
    {
        var normalized = (role ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "BRANCH_MANAGER" => "BRANCH_MANAGER",
            "HR" => "HR",
            "CASHIER" => "CASHIER",
            _ => null
        };
    }

    private Task<bool> UsernameExistsInTenantAsync(Guid tenantId, string username, CancellationToken ct)
    {
        return (
            from pin in _db.UserPins.IgnoreQueryFilters().AsNoTracking()
            join user in _db.Users.AsNoTracking() on pin.UserId equals user.Id
            where pin.TenantId == tenantId && user.Username == username
            select user.Id)
            .AnyAsync(ct);
    }

    private async Task<bool> UsernameExistsInBranchAsync(Guid tenantId, Guid branchId, string username, CancellationToken ct)
    {
        var count = await _db.Database.SqlQueryRaw<int>(
            """
            SELECT COUNT(1) AS Value
            FROM dbo.BranchUserAssignments AS assignment
            INNER JOIN dbo.Users AS u ON u.Id = assignment.UserId
            WHERE assignment.TenantId = {0}
              AND assignment.BranchId = {1}
              AND u.Username = {2}
            """,
            tenantId, branchId, username)
            .SingleAsync(ct);

        return count > 0;
    }

    private static DateTime CalculateLicenseExpiry(DateTime startedAt, string licensePlan)
        => licensePlan == "YEARLY"
            ? startedAt.AddYears(1)
            : startedAt.AddMonths(1);

    private async Task<bool> BranchExistsAsync(Guid tenantId, Guid branchId, bool requireActive, CancellationToken ct)
    {
        return await _db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                branch => branch.Id == branchId
                    && branch.TenantId == tenantId
                    && (!requireActive || branch.IsActive),
                ct);
    }
}
