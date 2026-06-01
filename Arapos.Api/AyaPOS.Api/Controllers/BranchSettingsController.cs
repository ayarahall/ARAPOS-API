using Ayapos.Api.Contracts.Platform;
using Ayapos.Api.Data;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantSlug}")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class BranchSettingsController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public BranchSettingsController(AyaposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("branch-settings/features")]
    public async Task<ActionResult<BranchFeatureSettingsDto>> GetFeatureSettings(CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant/branch context.");

        var settings = await _db.BranchSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value, ct);

        if (settings is null)
            return Ok(new BranchFeatureSettingsDto
            {
                AppointmentsRequireCustomer = true, AppointmentsPreventOverlap = true,
                AppointmentsAutoNoShow = true, AppointmentsCheckInCreatesInvoice = true,
                AppointmentsAllowNoShow = true, AppointmentsAllowCancel = true,
                ExpensesRequireApproval = true, ExpensesDeductCash = true,
                ExpensesNotifyApprovers = true, ExpensesAllowAiAssist = false,
                PosRequirePaymentReference = false, PosRequireAppointment = false,
                PosAutoPrintReceipt = false, PosAllowMultipleInvoiceTabs = true,
            });

        return Ok(new BranchFeatureSettingsDto
        {
            AppointmentsRequireCustomer = settings.AppointmentsRequireCustomer,
            AppointmentsPreventOverlap = settings.AppointmentsPreventOverlap,
            AppointmentsAutoNoShow = settings.AppointmentsAutoNoShow,
            AppointmentsCheckInCreatesInvoice = settings.AppointmentsCheckInCreatesInvoice,
            AppointmentsAllowNoShow = settings.AppointmentsAllowNoShow,
            AppointmentsAllowCancel = settings.AppointmentsAllowCancel,
            ExpensesRequireApproval = settings.ExpensesRequireApproval,
            ExpensesDeductCash = settings.ExpensesDeductCash,
            ExpensesNotifyApprovers = settings.ExpensesNotifyApprovers,
            ExpensesAllowAiAssist = settings.ExpensesAllowAiAssist,
            PosRequirePaymentReference = settings.PosRequirePaymentReference,
            PosRequireAppointment = settings.PosRequireAppointment,
            PosAutoPrintReceipt = settings.PosAutoPrintReceipt,
            PosAllowMultipleInvoiceTabs = settings.PosAllowMultipleInvoiceTabs,
        });
    }
}
