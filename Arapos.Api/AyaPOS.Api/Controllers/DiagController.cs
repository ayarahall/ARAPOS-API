using Ayapos.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

/// <summary>
/// Read-only production diagnostic — returns counts only, no PII.
/// Remove after debugging is complete.
/// </summary>
[ApiController]
[Route("diag")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class DiagController : ControllerBase
{
    private readonly AyaposDbContext _db;

    public DiagController(AyaposDbContext db) => _db = db;

    [HttpGet("db")]
    public async Task<IActionResult> DbInfo(CancellationToken ct)
    {
        var provider = _db.Database.ProviderName ?? "unknown";
        var sqlserverUrlSet = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SQLSERVER_URL"));
        var databaseUrlSet = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"));
        var activeSource = sqlserverUrlSet ? "SQLSERVER_URL" : databaseUrlSet ? "DATABASE_URL" : "appsettings";

        var tenantCount = await _db.Tenants.IgnoreQueryFilters().CountAsync(ct);
        var branchCount = await _db.Branches.IgnoreQueryFilters().CountAsync(ct);

        var kiraz = await _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.Slug == "kiraz")
            .Select(t => new { t.Id, t.Slug, t.Status, t.LicenseStatus })
            .FirstOrDefaultAsync(ct);

        object? kirazDetail = null;
        if (kiraz is not null)
        {
            var tid = kiraz.Id;
            var abuDhabiBranchId = new Guid("29c6e565-5c1a-41a3-bc6b-2a7a9bd2d701");

            var serviceCount = await _db.Services
                .IgnoreQueryFilters()
                .CountAsync(s => s.TenantId == tid, ct);

            var activePriceCount = await _db.ServicePrices
                .IgnoreQueryFilters()
                .CountAsync(sp => sp.TenantId == tid && sp.IsActive, ct);

            var inactivePriceCount = await _db.ServicePrices
                .IgnoreQueryFilters()
                .CountAsync(sp => sp.TenantId == tid && !sp.IsActive, ct);

            var servicesWithActivePrice = await _db.Services
                .IgnoreQueryFilters()
                .Join(_db.ServicePrices.IgnoreQueryFilters(),
                    s => s.Id, sp => sp.ServiceId,
                    (s, sp) => new { s, sp })
                .CountAsync(x =>
                    x.s.TenantId == tid &&
                    x.s.BranchId == abuDhabiBranchId &&
                    x.sp.TenantId == tid &&
                    x.sp.BranchId == abuDhabiBranchId &&
                    x.sp.IsActive, ct);

            var customerCount = await _db.Customers
                .IgnoreQueryFilters()
                .CountAsync(c => c.TenantId == tid, ct);

            var userPinCount = await _db.UserPins
                .IgnoreQueryFilters()
                .CountAsync(p => p.TenantId == tid, ct);

            var branchExists = await _db.Branches
                .IgnoreQueryFilters()
                .AnyAsync(b => b.TenantId == tid && b.Id == abuDhabiBranchId, ct);

            kirazDetail = new
            {
                tenantId = tid,
                slug = kiraz.Slug,
                status = kiraz.Status,
                licenseStatus = kiraz.LicenseStatus,
                abuDhabiBranchExists = branchExists,
                serviceRowCount = serviceCount,
                servicePricesActive = activePriceCount,
                servicePricesInactive = inactivePriceCount,
                servicesVisibleInApi = servicesWithActivePrice,
                customerRowCount = customerCount,
                usersWithPin = userPinCount,
            };
        }

        return Ok(new
        {
            provider,
            activeSource,
            sqlserverUrlSet,
            databaseUrlSet,
            tenantCount,
            branchCount,
            kiraz = kirazDetail
        });
    }
}
