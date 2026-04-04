using Arapos.Api.Data;
using Arapos.Api.Data.Entities;
using Arapos.Api.Security;
using Arapos.Api.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/cashier")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
public class CashierController : ControllerBase
{
    private readonly AraposDbContext _db;
    private readonly ITenantContext _tenant;

    public CashierController(AraposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================
    // Open Session
    // =========================
    [HttpPost("open")]
    public async Task<IActionResult> OpenSession([FromBody] int openingCashCents)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var session = new CashierSession
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            BranchId = _tenant.BranchId.Value,
            UserId = Guid.Empty, // لاحقاً نربطه بالمستخدم الحقيقي
            OpenedAt = DateTime.UtcNow,
            OpeningCashCents = openingCashCents,
            IsClosed = false
        };

        _db.CashierSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(session.Id);
    }

    // =========================
    // Close Session
    // =========================
    [HttpPost("{sessionId:guid}/close")]
    public async Task<IActionResult> CloseSession(Guid sessionId, [FromBody] int actualCashCents)
    {
        var session = await _db.CashierSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId);

        if (session == null || session.IsClosed)
            return BadRequest("Invalid session.");

        var now = DateTime.UtcNow;

        var payments = await _db.Payments
       .Where(p =>
           p.TenantId == session.TenantId &&
           p.PaidAt >= session.OpenedAt &&
           p.PaidAt <= now &&
           _db.Invoices.Any(i =>
               i.Id == p.InvoiceId &&
               i.BranchId == session.BranchId))
       .ToListAsync();


        session.TotalCashCents = payments
            .Where(p => p.Method == PaymentMethod.Cash)
            .Sum(p => p.AmountCents);

        session.TotalCardCents = payments
            .Where(p => p.Method == PaymentMethod.Card)
            .Sum(p => p.AmountCents);

        session.TotalTransferCents = payments
        .Where(p => p.Method == PaymentMethod.BankTransfer)
            .Sum(p => p.AmountCents);

        var refunds = await _db.Refunds
            .Where(r =>
                r.TenantId == session.TenantId &&
                r.RefundedAt >= session.OpenedAt &&
                r.RefundedAt <= now)
            .SumAsync(r => (int?)r.AmountCents) ?? 0;

        session.TotalRefundCents = refunds;

        session.ExpectedCashCents =
            session.OpeningCashCents +
            session.TotalCashCents -
            session.TotalRefundCents;

        session.ActualCashCents = actualCashCents;

        session.DifferenceCents =
            session.ActualCashCents - session.ExpectedCashCents;

        session.ClosedAt = now;
        session.IsClosed = true;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            session.TotalCashCents,
            session.TotalCardCents,
            session.TotalTransferCents,
            session.TotalRefundCents,
            session.ExpectedCashCents,
            session.ActualCashCents,
            session.DifferenceCents
        });
    }
}