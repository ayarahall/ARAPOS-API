using System.Security.Claims;
using System.Globalization;
using System.IO.Compression;
using Ayapos.Api.Contracts.Platform;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("tenant-admin")]
[Authorize(Policy = AuthPolicies.TenantAdmin)]
public sealed class BranchServiceImportForm
{
    public IFormFile? File { get; init; }
}

public sealed class TenantAdminController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly PasswordHasherService _passwordHasher;

    public TenantAdminController(AyaposDbContext db, PasswordHasherService passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet("tenant")]
    public async Task<ActionResult<PlatformTenantSummaryDto>> GetTenantSummary(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var assignedUsers = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId.Value)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync(ct);

        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId.Value)
            .Select(x => new PlatformTenantSummaryDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Status = x.Status,
                LicensePlan = x.LicensePlan,
                LicenseStatus = x.LicenseStatus,
                MaxUsers = x.MaxUsers,
                AssignedUsers = assignedUsers,
                LicenseStartedAt = x.LicenseStartedAt,
                LicenseExpiresAt = x.LicenseExpiresAt
            })
            .FirstOrDefaultAsync(ct);

        return tenant is null ? NotFound("Tenant not found.") : Ok(tenant);
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IReadOnlyList<PlatformBranchDto>>> ListBranches(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE [Branches] SET [IsActive] = 1 WHERE [TenantId] = {0} AND [IsActive] IS NULL",
            tenantId.Value);

        await EnsureTenantBranchOperationalSetupAsync(tenantId.Value, ct);

        var items = await _db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(branch => branch.TenantId == tenantId.Value)
            .OrderBy(branch => EF.Property<string?>(branch, nameof(Branch.Name)) ?? string.Empty)
            .Select(branch => new PlatformBranchDto
            {
                Id = branch.Id,
                TenantId = branch.TenantId,
                Name = EF.Property<string?>(branch, nameof(Branch.Name)) ?? string.Empty,
                Code = EF.Property<string?>(branch, nameof(Branch.Code)) ?? string.Empty,
                CurrencyCode = EF.Property<string?>(branch, nameof(Branch.CurrencyCode)) ?? "AED",
                IsActive = EF.Property<bool?>(branch, nameof(Branch.IsActive)) ?? true,
                HasPosWorkspace = _db.InvoiceSequences
                    .AsNoTracking()
                    .Any(sequence => sequence.TenantId == branch.TenantId && sequence.BranchId == branch.Id),
                HasAppointmentsWorkspace = true,
                HasExpensesWorkspace = true,
                HasPrintSettings = _db.BranchSettings
                    .AsNoTracking()
                    .Any(setting => setting.TenantId == branch.TenantId && setting.BranchId == branch.Id),
                NextInvoiceNumber = _db.InvoiceSequences
                    .AsNoTracking()
                    .Where(sequence => sequence.TenantId == branch.TenantId && sequence.BranchId == branch.Id)
                    .Select(sequence => (int?)sequence.NextNumber)
                    .FirstOrDefault() ?? 1,
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

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest req, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var code = (req.Code ?? "").Trim();
        var name = (req.Name ?? "").Trim();
        var currency = (req.CurrencyCode ?? "AED").Trim().ToUpperInvariant();

        if (code.Length < 1) return BadRequest("Code is required.");
        if (name.Length < 2) return BadRequest("Name is required.");
        if (currency.Length != 3) return BadRequest("CurrencyCode must be 3 letters.");

        var duplicate = await _db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(b => b.TenantId == tenantId.Value && b.Code == code, ct);

        if (duplicate)
            return Conflict("Branch code already exists for this tenant.");

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Code = code,
            Name = name,
            CurrencyCode = currency,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Branches.Add(branch);
        AddBranchOperationalSetup(branch);
        await _db.SaveChangesAsync(ct);

        return Ok(new PlatformBranchDto
        {
            Id = branch.Id,
            TenantId = branch.TenantId,
            Name = branch.Name,
            Code = branch.Code,
            CurrencyCode = branch.CurrencyCode,
            IsActive = branch.IsActive,
            AssignedUsers = 0,
            HasPosWorkspace = true,
            HasAppointmentsWorkspace = true,
            HasExpensesWorkspace = true,
            HasPrintSettings = true,
            NextInvoiceNumber = 1
        });
    }

    [HttpGet("branches/{branchId:guid}/print-settings")]
    public async Task<ActionResult<BranchInvoicePrintSettingsDto>> GetBranchPrintSettings([FromRoute] Guid branchId, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branch = await _db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == branchId && x.TenantId == tenantId.Value, ct);

        if (branch is null)
            return NotFound("Branch not found.");

        var settings = await _db.BranchSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.BranchId == branchId, ct);

        settings ??= CreateDefaultBranchSetting(tenantId.Value, branchId, branch.Name);

        if (_db.Entry(settings).State == EntityState.Detached)
        {
            _db.BranchSettings.Add(settings);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(MapPrintSettings(settings));
    }

    [HttpPost("branches/{branchId:guid}/print-settings")]
    public async Task<ActionResult<BranchInvoicePrintSettingsDto>> UpdateBranchPrintSettings(
        [FromRoute] Guid branchId,
        [FromBody] UpdateBranchInvoicePrintSettingsRequest req,
        CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branch = await _db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == branchId && x.TenantId == tenantId.Value, ct);

        if (branch is null)
            return NotFound("Branch not found.");

        var settings = await _db.BranchSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.BranchId == branchId, ct);

        if (settings is null)
        {
            settings = CreateDefaultBranchSetting(tenantId.Value, branchId, branch.Name);
            _db.BranchSettings.Add(settings);
        }

        settings.CompanyName = NormalizeOptionalText(req.CompanyName, 160);
        settings.CompanyLogoUrl = NormalizeOptionalText(req.CompanyLogoUrl, 500);
        settings.CompanyPhone = NormalizeOptionalText(req.CompanyPhone, 40);
        settings.CompanyAddress = NormalizeOptionalText(req.CompanyAddress, 220);
        settings.CompanyTaxNumber = NormalizeOptionalText(req.CompanyTaxNumber, 80);
        settings.ReceiptTitle = NormalizeReceiptTitle(req.ReceiptTitle);
        settings.ReceiptHeaderLine1 = NormalizeOptionalText(req.ReceiptHeaderLine1, 120);
        settings.ReceiptHeaderLine2 = NormalizeOptionalText(req.ReceiptHeaderLine2, 120);
        settings.ReceiptFooterNote = NormalizeOptionalText(req.ReceiptFooterNote, 220);
        settings.ShowBranchNameOnReceipt = req.ShowBranchNameOnReceipt;
        settings.ShowCustomerNameOnReceipt = req.ShowCustomerNameOnReceipt;
        settings.ShowPaymentHistoryOnReceipt = req.ShowPaymentHistoryOnReceipt;
        settings.AutoPrintReceiptAfterPayment = req.AutoPrintReceiptAfterPayment;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(MapPrintSettings(settings));
    }

    [HttpPost("branches/{branchId:guid}")]
    public async Task<IActionResult> UpdateBranch([FromRoute] Guid branchId, [FromBody] UpdateBranchRequest req, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.TenantId == tenantId.Value, ct);
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
            .AnyAsync(b => b.TenantId == tenantId.Value && b.Code == code && b.Id != branchId, ct);

        if (duplicate)
            return Conflict("Branch code already exists for this tenant.");

        branch.Code = code;
        branch.Name = name;
        branch.CurrencyCode = currency;
        branch.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);

        var assignedUsers = await _db.BranchUserAssignments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId)
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

    [HttpGet("branches/{branchId:guid}/users")]
    public async Task<ActionResult<IReadOnlyList<PlatformBranchUserDto>>> ListBranchUsers([FromRoute] Guid branchId, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branchExists = await BranchExistsAsync(tenantId.Value, branchId, requireActive: false, ct);
        if (!branchExists)
            return NotFound("Branch not found.");

        var items = await (
            from pin in _db.UserPins.IgnoreQueryFilters().AsNoTracking()
            join user in _db.Users.AsNoTracking() on pin.UserId equals user.Id
            where pin.TenantId == tenantId.Value
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
                CreatedAt = user.CreatedAt,
                Permissions = UserPermissionCatalog.GetEffectivePermissions(user),
                PermissionsConfigured = UserPermissionCatalog.HasExplicitPermissions(user)
            })
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("branches/{branchId:guid}/users")]
    public async Task<IActionResult> CreateBranchUser([FromRoute] Guid branchId, [FromBody] CreateBranchUserRequest req, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId.Value && t.Status == "ACTIVE", ct);
        if (tenant is null) return NotFound("Tenant not found.");

        var branchExists = await BranchExistsAsync(tenantId.Value, branchId, requireActive: true, ct);
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

        var userExists = await UsernameExistsInBranchAsync(tenantId.Value, branchId, username, ct);
        if (userExists) return Conflict("Username already exists for this branch.");

        var assignedUsersCount = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId.Value)
            .Select(p => p.UserId)
            .Distinct()
            .CountAsync(ct);

        if (assignedUsersCount >= tenant.MaxUsers)
            return BadRequest($"Tenant has reached its maximum allowed users ({tenant.MaxUsers}).");

        var licenseStartedAt = DateTime.UtcNow;
        var licenseExpiresAt = licensePlan == "YEARLY" ? licenseStartedAt.AddYears(1) : licenseStartedAt.AddMonths(1);

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
            PasswordHash = string.IsNullOrWhiteSpace(password) ? "PIN_ONLY" : _passwordHasher.Hash(password),
            PinHash = null,
            PermissionsJson = UserPermissionCatalog.SerializePermissions(UserPermissionCatalog.GetDefaultPermissionsForRole(role))
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO BranchUserAssignments (UserId, TenantId, BranchId, AssignedAt)
            VALUES ({0}, {1}, {2}, {3})
            """,
            user.Id, tenantId.Value, branchId, DateTime.UtcNow);

        await _db.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_SetUserPinV2 @UserId={0}, @TenantId={1}, @Pin={2}",
            user.Id, tenantId.Value, pin);

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
            CreatedAt = user.CreatedAt,
            Permissions = UserPermissionCatalog.GetEffectivePermissions(user),
            PermissionsConfigured = UserPermissionCatalog.HasExplicitPermissions(user)
        });
    }

    [HttpPost("branches/{branchId:guid}/users/{userId:guid}/license")]
    public async Task<IActionResult> UpdateBranchUserLicense([FromRoute] Guid branchId, [FromRoute] Guid userId, [FromBody] UpdateTenantUserLicenseRequest req, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branchExists = await BranchExistsAsync(tenantId.Value, branchId, requireActive: false, ct);
        if (!branchExists)
            return NotFound("Branch not found.");

        var userBelongsToTenant = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId.Value, ct);

        if (!userBelongsToTenant)
            return NotFound("Tenant user not found.");

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
        user.LicenseExpiresAt = licensePlan == "YEARLY" ? licenseStartedAt.AddYears(1) : licenseStartedAt.AddMonths(1);

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
            CreatedAt = user.CreatedAt,
            Permissions = UserPermissionCatalog.GetEffectivePermissions(user),
            PermissionsConfigured = UserPermissionCatalog.HasExplicitPermissions(user)
        });
    }

    [HttpPost("branches/{branchId:guid}/users/{userId:guid}/permissions")]
    public async Task<IActionResult> UpdateBranchUserPermissions(
        [FromRoute] Guid branchId,
        [FromRoute] Guid userId,
        [FromBody] UpdateBranchUserPermissionsRequest req,
        CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branchExists = await BranchExistsAsync(tenantId.Value, branchId, requireActive: false, ct);
        if (!branchExists)
            return NotFound("Branch not found.");

        var userBelongsToTenant = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId.Value, ct);

        if (!userBelongsToTenant)
            return NotFound("Tenant user not found.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return NotFound("User not found.");

        user.PermissionsJson = UserPermissionCatalog.SerializePermissions(req.Permissions);
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
            CreatedAt = user.CreatedAt,
            Permissions = UserPermissionCatalog.GetEffectivePermissions(user),
            PermissionsConfigured = UserPermissionCatalog.HasExplicitPermissions(user)
        });
    }

    [HttpPost("branches/{branchId:guid}/users/{userId:guid}/password")]
    public async Task<IActionResult> SetBranchUserPassword([FromRoute] Guid branchId, [FromRoute] Guid userId, [FromBody] SetTenantUserPasswordRequest req, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var newPassword = (req.NewPassword ?? "").Trim();
        if (newPassword.Length < 6)
            return BadRequest("New password must be at least 6 characters.");

        var branchExists = await BranchExistsAsync(tenantId.Value, branchId, requireActive: false, ct);
        if (!branchExists)
            return NotFound("Branch not found.");

        var userBelongsToTenant = await _db.UserPins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId.Value, ct);

        if (!userBelongsToTenant)
            return NotFound("Tenant user not found.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user is null)
            return NotFound("User not found.");

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = "Password set successfully.", user.Id, BranchId = branchId });
    }

    [HttpPost("branches/{branchId:guid}/services/import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ServiceImportResultDto>> ImportBranchServices(
        [FromRoute] Guid branchId,
        [FromForm] BranchServiceImportForm request,
        CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branch = await _db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == branchId && x.TenantId == tenantId.Value && x.IsActive, ct);

        if (branch is null)
            return NotFound("Branch not found.");

        var file = request.File;
        if (file is null || file.Length == 0)
            return BadRequest("Excel file is required.");

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx and .csv files are supported.");

        List<ServiceImportRowDto> rows;
        await using (var stream = file.OpenReadStream())
        {
            rows = string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
                ? ReadServiceImportRowsFromCsv(stream)
                : ReadServiceImportRows(stream);
        }

        if (rows.Count == 0)
            return BadRequest("No importable rows were found in the Excel file.");

        var existingServices = await _db.Services
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId)
            .ToListAsync(ct);

        var existingPrices = await _db.ServicePrices
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId)
            .ToListAsync(ct);

        var serviceByKey = existingServices.ToDictionary(CreateServiceLookupKey, StringComparer.OrdinalIgnoreCase);
        var priceByServiceId = existingPrices.ToDictionary(x => x.ServiceId);

        var result = new ServiceImportResultDto
        {
            TotalRows = rows.Count
        };

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.NameAr) && string.IsNullOrWhiteSpace(row.NameEn))
            {
                result.Issues.Add(new ServiceImportIssueDto
                {
                    RowNumber = row.RowNumber,
                    Message = "Arabic or English service name is required."
                });
                continue;
            }

            if (row.Price is null || row.Price < 0)
            {
                result.Issues.Add(new ServiceImportIssueDto
                {
                    RowNumber = row.RowNumber,
                    Message = "Price must be a numeric AED value."
                });
                continue;
            }

            if (row.DurationMin is not null && row.DurationMin < 0)
            {
                result.Issues.Add(new ServiceImportIssueDto
                {
                    RowNumber = row.RowNumber,
                    Message = "DurationMin cannot be negative."
                });
                continue;
            }

            var key = CreateServiceLookupKey(row.NameAr, row.NameEn);
            if (serviceByKey.TryGetValue(key, out var service))
            {
                service.NameAr = NormalizeOptionalText(row.NameAr, 120);
                service.NameEn = NormalizeOptionalText(row.NameEn, 120);
                service.DurationMin = row.DurationMin;
                service.IsActive = true;

                if (!priceByServiceId.TryGetValue(service.Id, out var existingPrice))
                {
                    existingPrice = new ServicePrice
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId.Value,
                        BranchId = branchId,
                        ServiceId = service.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _db.ServicePrices.Add(existingPrice);
                    priceByServiceId[service.Id] = existingPrice;
                }

                existingPrice.PriceCents = DecimalToCents(row.Price.Value);
                existingPrice.CurrencyCode = NormalizeCurrencyCode(row.CurrencyCode, branch.CurrencyCode);
                existingPrice.IsActive = true;
                result.UpdatedCount++;
                continue;
            }

            service = new Service
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                BranchId = branchId,
                NameAr = NormalizeOptionalText(row.NameAr, 120),
                NameEn = NormalizeOptionalText(row.NameEn, 120),
                DurationMin = row.DurationMin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var servicePrice = new ServicePrice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                BranchId = branchId,
                ServiceId = service.Id,
                PriceCents = DecimalToCents(row.Price.Value),
                CurrencyCode = NormalizeCurrencyCode(row.CurrencyCode, branch.CurrencyCode),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Services.Add(service);
            _db.ServicePrices.Add(servicePrice);

            serviceByKey[key] = service;
            priceByServiceId[service.Id] = servicePrice;
            result.CreatedCount++;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new ServiceImportResultDto
        {
            TotalRows = result.TotalRows,
            CreatedCount = result.CreatedCount,
            UpdatedCount = result.UpdatedCount,
            SkippedCount = result.Issues.Count,
            Issues = result.Issues
        });
    }

    [HttpPost("branches/{branchId:guid}/products/import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ServiceImportResultDto>> ImportBranchProducts(
        [FromRoute] Guid branchId,
        [FromForm] BranchServiceImportForm request,
        CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branch = await _db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == branchId && x.TenantId == tenantId.Value && x.IsActive, ct);

        if (branch is null)
            return NotFound("Branch not found.");

        var file = request.File;
        if (file is null || file.Length == 0)
            return BadRequest("Excel file is required.");

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx and .csv files are supported.");

        List<ProductImportRowDto> rows;
        await using (var stream = file.OpenReadStream())
        {
            rows = string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
                ? ReadProductImportRowsFromCsv(stream)
                : ReadProductImportRows(stream);
        }

        if (rows.Count == 0)
            return BadRequest("No importable rows were found in the file.");

        var existingProducts = await _db.Products
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId)
            .ToListAsync(ct);

        var productByKey = existingProducts.ToDictionary(CreateProductLookupKey, StringComparer.OrdinalIgnoreCase);
        var productBySku = existingProducts
            .Where(x => !string.IsNullOrWhiteSpace(x.Sku))
            .ToDictionary(x => x.Sku!.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

        var result = new ServiceImportResultDto { TotalRows = rows.Count };

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.NameAr) && string.IsNullOrWhiteSpace(row.NameEn))
            {
                result.Issues.Add(new ServiceImportIssueDto
                {
                    RowNumber = row.RowNumber,
                    Message = "Arabic or English product name is required."
                });
                continue;
            }

            if (row.Price is null || row.Price < 0)
            {
                result.Issues.Add(new ServiceImportIssueDto
                {
                    RowNumber = row.RowNumber,
                    Message = "Price must be a non-negative numeric value."
                });
                continue;
            }

            var skuKey = (row.Sku ?? string.Empty).Trim().ToUpperInvariant();
            var nameKey = CreateProductLookupKey(row.NameAr, row.NameEn);

            Product? product = null;
            if (skuKey.Length > 0 && productBySku.TryGetValue(skuKey, out product))
            {
                // matched by SKU
            }
            else if (productByKey.TryGetValue(nameKey, out product))
            {
                // matched by name
            }

            if (product is not null)
            {
                product.NameAr = NormalizeOptionalText(row.NameAr, 120);
                product.NameEn = NormalizeOptionalText(row.NameEn, 120);
                if (!string.IsNullOrWhiteSpace(row.Sku)) product.Sku = NormalizeOptionalText(row.Sku, 100);
                if (!string.IsNullOrWhiteSpace(row.Barcode)) product.Barcode = NormalizeOptionalText(row.Barcode, 100);
                if (!string.IsNullOrWhiteSpace(row.Unit)) product.Unit = row.Unit.Trim()[..Math.Min(row.Unit.Trim().Length, 50)];
                product.SellPrice = row.Price.Value;
                if (row.CostPrice.HasValue && row.CostPrice.Value >= 0) product.CostPrice = row.CostPrice.Value;
                product.CurrencyCode = NormalizeCurrencyCode(row.CurrencyCode, branch.CurrencyCode);
                product.IsActive = true;
                result.UpdatedCount++;
            }
            else
            {
                product = new Product
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    NameAr = NormalizeOptionalText(row.NameAr, 120),
                    NameEn = NormalizeOptionalText(row.NameEn, 120),
                    Sku = NormalizeOptionalText(row.Sku, 100),
                    Barcode = NormalizeOptionalText(row.Barcode, 100),
                    Unit = NormalizeOptionalText(row.Unit, 50) ?? string.Empty,
                    SellPrice = row.Price.Value,
                    CostPrice = row.CostPrice ?? 0m,
                    CurrencyCode = NormalizeCurrencyCode(row.CurrencyCode, branch.CurrencyCode),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Products.Add(product);

                if (skuKey.Length > 0) productBySku[skuKey] = product;
                productByKey[nameKey] = product;
                result.CreatedCount++;
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new ServiceImportResultDto
        {
            TotalRows = result.TotalRows,
            CreatedCount = result.CreatedCount,
            UpdatedCount = result.UpdatedCount,
            SkippedCount = result.Issues.Count,
            Issues = result.Issues
        });
    }

    private Guid? GetTenantId()
    {
        var tenantIdClaim = User.FindFirstValue("tenantId");
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

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

    private async Task EnsureTenantBranchOperationalSetupAsync(Guid tenantId, CancellationToken ct)
    {
        var branches = await _db.Branches
            .IgnoreQueryFilters()
            .Where(branch => branch.TenantId == tenantId)
            .ToListAsync(ct);

        var branchIds = branches.Select(branch => branch.Id).ToHashSet();

        var existingSettingBranchIds = await _db.BranchSettings
            .AsNoTracking()
            .Where(setting => setting.TenantId == tenantId && branchIds.Contains(setting.BranchId))
            .Select(setting => setting.BranchId)
            .ToListAsync(ct);

        var existingSequenceBranchIds = await _db.InvoiceSequences
            .AsNoTracking()
            .Where(sequence => sequence.TenantId == tenantId && branchIds.Contains(sequence.BranchId))
            .Select(sequence => sequence.BranchId)
            .ToListAsync(ct);

        var existingSettingSet = existingSettingBranchIds.ToHashSet();
        var existingSequenceSet = existingSequenceBranchIds.ToHashSet();
        var changed = false;

        foreach (var branch in branches)
        {
            if (!existingSettingSet.Contains(branch.Id))
            {
                _db.BranchSettings.Add(CreateDefaultBranchSetting(branch.TenantId, branch.Id, branch.Name));
                changed = true;
            }

            if (!existingSequenceSet.Contains(branch.Id))
            {
                _db.InvoiceSequences.Add(CreateDefaultInvoiceSequence(branch.TenantId, branch.Id));
                changed = true;
            }
        }

        if (changed)
            await _db.SaveChangesAsync(ct);
    }

    private void AddBranchOperationalSetup(Branch branch)
    {
        _db.BranchSettings.Add(CreateDefaultBranchSetting(branch.TenantId, branch.Id, branch.Name));
        _db.InvoiceSequences.Add(CreateDefaultInvoiceSequence(branch.TenantId, branch.Id));
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

    private static BranchSetting CreateDefaultBranchSetting(Guid tenantId, Guid branchId, string? branchName = null)
        => new()
        {
            TenantId = tenantId,
            BranchId = branchId,
            MaxCashierDiscountPct = 0,
            AllowLineDiscount = true,
            AllowInvoiceDiscount = true,
            RequireManagerForPriceOverride = true,
            CompanyName = string.IsNullOrWhiteSpace(branchName) ? null : branchName.Trim(),
            CompanyLogoUrl = null,
            CompanyPhone = null,
            CompanyAddress = null,
            CompanyTaxNumber = null,
            ReceiptTitle = "Sales Receipt",
            ReceiptHeaderLine1 = string.IsNullOrWhiteSpace(branchName) ? null : branchName.Trim(),
            ReceiptHeaderLine2 = null,
            ReceiptFooterNote = "Thank you for visiting AYAPOS.",
            ShowBranchNameOnReceipt = true,
            ShowCustomerNameOnReceipt = true,
            ShowPaymentHistoryOnReceipt = true,
            AutoPrintReceiptAfterPayment = false,
            UpdatedAt = DateTime.UtcNow
        };

    private static InvoiceSequence CreateDefaultInvoiceSequence(Guid tenantId, Guid branchId)
        => new()
        {
            TenantId = tenantId,
            BranchId = branchId,
            NextNumber = 1,
            UpdatedAt = DateTime.UtcNow
        };

    private static int DecimalToCents(decimal amount)
        => (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static string CreateServiceLookupKey(Service service)
        => CreateServiceLookupKey(service.NameAr, service.NameEn);

    private static string CreateServiceLookupKey(string? nameAr, string? nameEn)
        => $"{(nameAr ?? string.Empty).Trim().ToUpperInvariant()}|{(nameEn ?? string.Empty).Trim().ToUpperInvariant()}";

    private static string NormalizeCurrencyCode(string? currencyCode, string fallback)
    {
        var normalized = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        return normalized.Length == 3 ? normalized : fallback.Trim().ToUpperInvariant();
    }

    private static List<ServiceImportRowDto> ReadServiceImportRows(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException("The Excel file must contain data in the first worksheet.");

        using var sheetStream = sheetEntry.Open();
        var document = System.Xml.Linq.XDocument.Load(sheetStream);
        System.Xml.Linq.XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var rows = document.Descendants(ns + "row")
            .Select(row => row.Elements(ns + "c")
                .ToDictionary(
                    cell => GetColumnIndex((string?)cell.Attribute("r")),
                    cell => GetCellValue(cell, sharedStrings),
                    EqualityComparer<int>.Default))
            .Where(row => row.Count > 0)
            .ToList();

        if (rows.Count == 0)
            return [];

        var headerMap = BuildHeaderMap(rows[0]);
        if (!headerMap.TryGetValue("arabic", out var arabicIndex) ||
            !headerMap.TryGetValue("english", out var englishIndex) ||
            !headerMap.TryGetValue("price", out var priceIndex))
        {
            throw new InvalidOperationException("Excel headers must include Arabic, English, and Price columns.");
        }

        var hasDuration = headerMap.TryGetValue("duration", out var durationIndex);
        var hasCurrency = headerMap.TryGetValue("currency", out var currencyIndex);

        var result = new List<ServiceImportRowDto>();
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var nameAr = GetRowValue(row, arabicIndex);
            var nameEn = GetRowValue(row, englishIndex);
            var priceText = GetRowValue(row, priceIndex);

            if (string.IsNullOrWhiteSpace(nameAr) && string.IsNullOrWhiteSpace(nameEn) && string.IsNullOrWhiteSpace(priceText))
                continue;

            result.Add(new ServiceImportRowDto
            {
                RowNumber = i + 1,
                NameAr = NormalizeOptionalText(nameAr, 120),
                NameEn = NormalizeOptionalText(nameEn, 120),
                Price = TryParseDecimal(priceText),
                DurationMin = TryParseInt(hasDuration ? GetRowValue(row, durationIndex) : null),
                CurrencyCode = hasCurrency ? (GetRowValue(row, currencyIndex) ?? "AED") : "AED"
            });
        }

        return result;
    }

    private static List<ServiceImportRowDto> ReadServiceImportRowsFromCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is not null)
                lines.Add(line);
        }

        if (lines.Count == 0)
            return [];

        var headerCells = ParseCsvLine(lines[0]);
        var headerRow = new Dictionary<int, string>();
        for (var i = 0; i < headerCells.Count; i++)
            headerRow[i + 1] = headerCells[i];

        var headerMap = BuildHeaderMap(headerRow);
        if (!headerMap.TryGetValue("arabic", out var arabicIndex) ||
            !headerMap.TryGetValue("english", out var englishIndex) ||
            !headerMap.TryGetValue("price", out var priceIndex))
        {
            throw new InvalidOperationException("CSV headers must include Arabic, English, and Price columns.");
        }

        var hasDuration = headerMap.TryGetValue("duration", out var durationIndex);
        var hasCurrency = headerMap.TryGetValue("currency", out var currencyIndex);

        var result = new List<ServiceImportRowDto>();
        for (var rowIndex = 1; rowIndex < lines.Count; rowIndex++)
        {
            var cells = ParseCsvLine(lines[rowIndex]);
            if (cells.All(string.IsNullOrWhiteSpace))
                continue;

            string? GetCell(int oneBasedIndex)
                => oneBasedIndex >= 1 && oneBasedIndex <= cells.Count
                    ? cells[oneBasedIndex - 1]?.Trim()
                    : null;

            var nameAr = GetCell(arabicIndex);
            var nameEn = GetCell(englishIndex);
            var priceText = GetCell(priceIndex);

            if (string.IsNullOrWhiteSpace(nameAr) && string.IsNullOrWhiteSpace(nameEn) && string.IsNullOrWhiteSpace(priceText))
                continue;

            result.Add(new ServiceImportRowDto
            {
                RowNumber = rowIndex + 1,
                NameAr = NormalizeOptionalText(nameAr, 120),
                NameEn = NormalizeOptionalText(nameEn, 120),
                Price = TryParseDecimal(priceText),
                DurationMin = TryParseInt(hasDuration ? GetCell(durationIndex) : null),
                CurrencyCode = hasCurrency ? (GetCell(currencyIndex) ?? "AED") : "AED"
            });
        }

        return result;
    }

    private static Dictionary<string, int> BuildHeaderMap(Dictionary<int, string> headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in headerRow)
        {
            var normalized = NormalizeHeader(item.Value);
            if (normalized.Length == 0)
                continue;

            if (normalized is "arabic" or "english" or "price" or "duration" or "currency")
                map[normalized] = item.Key;
        }

        return map;
    }

    private static string NormalizeHeader(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "arabic" => "arabic",
            "arabic name" => "arabic",
            "name ar" => "arabic",
            "namear" => "arabic",
            "english" => "english",
            "english name" => "english",
            "name en" => "english",
            "nameen" => "english",
            "price" => "price",
            "sell price" => "price",
            "duration" => "duration",
            "durationmin" => "duration",
            "duration (min)" => "duration",
            "currency" => "currency",
            "currency code" => "currency",
            "currencycode" => "currency",
            _ => string.Empty
        };
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }

    private static string? GetRowValue(Dictionary<int, string> row, int index)
        => row.TryGetValue(index, out var value) ? value?.Trim() : null;

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return [];

        using var stream = entry.Open();
        var document = System.Xml.Linq.XDocument.Load(stream);
        System.Xml.Linq.XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return document.Descendants(ns + "si")
            .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
            .ToList();
    }

    private static string GetCellValue(System.Xml.Linq.XElement cell, List<string> sharedStrings)
    {
        System.Xml.Linq.XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var type = (string?)cell.Attribute("t");
        var value = cell.Element(ns + "v")?.Value;

        if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(value, out var sharedIndex) &&
            sharedIndex >= 0 &&
            sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
            return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));

        return value ?? string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
            return -1;

        var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        if (letters.Length == 0)
            return -1;

        var value = 0;
        foreach (var letter in letters.ToUpperInvariant())
            value = (value * 26) + (letter - 'A' + 1);

        return value;
    }

    private static decimal? TryParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static int? TryParseInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static BranchInvoicePrintSettingsDto MapPrintSettings(BranchSetting settings)
        => new()
        {
            BranchId = settings.BranchId,
            CompanyName = settings.CompanyName,
            CompanyLogoUrl = settings.CompanyLogoUrl,
            CompanyPhone = settings.CompanyPhone,
            CompanyAddress = settings.CompanyAddress,
            CompanyTaxNumber = settings.CompanyTaxNumber,
            ReceiptTitle = NormalizeReceiptTitle(settings.ReceiptTitle),
            ReceiptHeaderLine1 = settings.ReceiptHeaderLine1,
            ReceiptHeaderLine2 = settings.ReceiptHeaderLine2,
            ReceiptFooterNote = settings.ReceiptFooterNote,
            ShowBranchNameOnReceipt = settings.ShowBranchNameOnReceipt,
            ShowCustomerNameOnReceipt = settings.ShowCustomerNameOnReceipt,
            ShowPaymentHistoryOnReceipt = settings.ShowPaymentHistoryOnReceipt,
            AutoPrintReceiptAfterPayment = settings.AutoPrintReceiptAfterPayment
        };

    private static string NormalizeReceiptTitle(string? title)
    {
        var normalized = (title ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "Sales Receipt" : normalized[..Math.Min(normalized.Length, 80)];
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return normalized[..Math.Min(normalized.Length, maxLength)];
    }

    private static string CreateProductLookupKey(Product product)
        => CreateProductLookupKey(product.NameAr, product.NameEn);

    private static string CreateProductLookupKey(string? nameAr, string? nameEn)
        => $"{(nameAr ?? string.Empty).Trim().ToUpperInvariant()}|{(nameEn ?? string.Empty).Trim().ToUpperInvariant()}";

    private static Dictionary<string, int> BuildProductHeaderMap(Dictionary<int, string> headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in headerRow)
        {
            var normalized = NormalizeProductHeader(item.Value);
            if (normalized.Length == 0)
                continue;

            map.TryAdd(normalized, item.Key);
        }

        return map;
    }

    private static string NormalizeProductHeader(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "arabic" or "arabic name" or "name ar" or "namear" => "arabic",
            "english" or "english name" or "name en" or "nameen" => "english",
            "price" or "sell price" or "sellprice" => "price",
            "sku" => "sku",
            "barcode" => "barcode",
            "unit" => "unit",
            "cost" or "cost price" or "costprice" => "cost",
            "currency" or "currency code" or "currencycode" => "currency",
            _ => string.Empty
        };
    }

    private static List<ProductImportRowDto> ReadProductImportRows(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException("The Excel file must contain data in the first worksheet.");

        using var sheetStream = sheetEntry.Open();
        var document = System.Xml.Linq.XDocument.Load(sheetStream);
        System.Xml.Linq.XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var rows = document.Descendants(ns + "row")
            .Select(row => row.Elements(ns + "c")
                .ToDictionary(
                    cell => GetColumnIndex((string?)cell.Attribute("r")),
                    cell => GetCellValue(cell, sharedStrings),
                    EqualityComparer<int>.Default))
            .Where(row => row.Count > 0)
            .ToList();

        if (rows.Count == 0)
            return [];

        var headerMap = BuildProductHeaderMap(rows[0]);
        if (!headerMap.TryGetValue("arabic", out var arabicIndex) ||
            !headerMap.TryGetValue("english", out var englishIndex) ||
            !headerMap.TryGetValue("price", out var priceIndex))
        {
            throw new InvalidOperationException("Excel headers must include Arabic, English, and Price columns.");
        }

        var hasSku = headerMap.TryGetValue("sku", out var skuIndex);
        var hasBarcode = headerMap.TryGetValue("barcode", out var barcodeIndex);
        var hasUnit = headerMap.TryGetValue("unit", out var unitIndex);
        var hasCost = headerMap.TryGetValue("cost", out var costIndex);
        var hasCurrency = headerMap.TryGetValue("currency", out var currencyIndex);

        var result = new List<ProductImportRowDto>();
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var nameAr = GetRowValue(row, arabicIndex);
            var nameEn = GetRowValue(row, englishIndex);
            var priceText = GetRowValue(row, priceIndex);

            if (string.IsNullOrWhiteSpace(nameAr) && string.IsNullOrWhiteSpace(nameEn) && string.IsNullOrWhiteSpace(priceText))
                continue;

            result.Add(new ProductImportRowDto
            {
                RowNumber = i + 1,
                NameAr = NormalizeOptionalText(nameAr, 120),
                NameEn = NormalizeOptionalText(nameEn, 120),
                Price = TryParseDecimal(priceText),
                Sku = hasSku ? NormalizeOptionalText(GetRowValue(row, skuIndex), 100) : null,
                Barcode = hasBarcode ? NormalizeOptionalText(GetRowValue(row, barcodeIndex), 100) : null,
                Unit = hasUnit ? NormalizeOptionalText(GetRowValue(row, unitIndex), 50) : null,
                CostPrice = hasCost ? TryParseDecimal(GetRowValue(row, costIndex)) : null,
                CurrencyCode = hasCurrency ? (GetRowValue(row, currencyIndex) ?? "AED") : "AED"
            });
        }

        return result;
    }

    private static List<ProductImportRowDto> ReadProductImportRowsFromCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is not null)
                lines.Add(line);
        }

        if (lines.Count == 0)
            return [];

        var headerCells = ParseCsvLine(lines[0]);
        var headerRow = new Dictionary<int, string>();
        for (var i = 0; i < headerCells.Count; i++)
            headerRow[i + 1] = headerCells[i];

        var headerMap = BuildProductHeaderMap(headerRow);
        if (!headerMap.TryGetValue("arabic", out var arabicIndex) ||
            !headerMap.TryGetValue("english", out var englishIndex) ||
            !headerMap.TryGetValue("price", out var priceIndex))
        {
            throw new InvalidOperationException("CSV headers must include Arabic, English, and Price columns.");
        }

        var hasSku = headerMap.TryGetValue("sku", out var skuIndex);
        var hasBarcode = headerMap.TryGetValue("barcode", out var barcodeIndex);
        var hasUnit = headerMap.TryGetValue("unit", out var unitIndex);
        var hasCost = headerMap.TryGetValue("cost", out var costIndex);
        var hasCurrency = headerMap.TryGetValue("currency", out var currencyIndex);

        var result = new List<ProductImportRowDto>();
        for (var rowIndex = 1; rowIndex < lines.Count; rowIndex++)
        {
            var cells = ParseCsvLine(lines[rowIndex]);
            if (cells.All(string.IsNullOrWhiteSpace))
                continue;

            string? GetCell(int oneBasedIndex)
                => oneBasedIndex >= 1 && oneBasedIndex <= cells.Count
                    ? cells[oneBasedIndex - 1]?.Trim()
                    : null;

            var nameAr = GetCell(arabicIndex);
            var nameEn = GetCell(englishIndex);
            var priceText = GetCell(priceIndex);

            if (string.IsNullOrWhiteSpace(nameAr) && string.IsNullOrWhiteSpace(nameEn) && string.IsNullOrWhiteSpace(priceText))
                continue;

            result.Add(new ProductImportRowDto
            {
                RowNumber = rowIndex + 1,
                NameAr = NormalizeOptionalText(nameAr, 120),
                NameEn = NormalizeOptionalText(nameEn, 120),
                Price = TryParseDecimal(priceText),
                Sku = hasSku ? NormalizeOptionalText(GetCell(skuIndex), 100) : null,
                Barcode = hasBarcode ? NormalizeOptionalText(GetCell(barcodeIndex), 100) : null,
                Unit = hasUnit ? NormalizeOptionalText(GetCell(unitIndex), 50) : null,
                CostPrice = hasCost ? TryParseDecimal(GetCell(costIndex)) : null,
                CurrencyCode = hasCurrency ? (GetCell(currencyIndex) ?? "AED") : "AED"
            });
        }

        return result;
    }
}
