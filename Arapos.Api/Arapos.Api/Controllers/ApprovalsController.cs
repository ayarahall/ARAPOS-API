using Arapos.Api.Data;
using Arapos.Api.Data.Entities;
using Arapos.Api.Security;
using Arapos.Api.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/approvals")]
[Authorize(Policy = AuthPolicies.TenantAdmin)]
public class ApprovalsController : ControllerBase
{
    private readonly AraposDbContext _db;
    private readonly ITenantContext _tenant;

    public ApprovalsController(AraposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =====================================================
    // Approve Refund
    // =====================================================
    [HttpPost("{approvalId:guid}/approve")]
    public async Task<IActionResult> ApproveRefund(Guid approvalId, CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var approval = await _db.ManagerApprovals
            .FirstOrDefaultAsync(a =>
                a.Id == approvalId &&
                a.TenantId == tenantId &&
                a.ApprovalType == "Refund",
                ct);

        if (approval is null)
            return NotFound("Approval not found.");

        if (approval.ApprovedAt != null)
            return BadRequest("Already approved.");

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i =>
                i.Id == approval.RefId &&
                i.TenantId == tenantId &&
                i.BranchId == branchId,
                ct);

        if (invoice is null)
            return NotFound("Invoice not found.");

        // استخراج مبلغ الريفند من Notes
        var amountText = approval.Notes.Replace("Refund request for ", "").Replace(" cents", "");
        if (!int.TryParse(amountText, out var amountCents))
            return BadRequest("Invalid refund amount.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // إنشاء Refund
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoice.Id,
            AmountCents = amountCents,
            RefundedAt = DateTime.UtcNow
        };

        _db.Refunds.Add(refund);

        // إعادة المخزون
        var items = await _db.InvoiceItems
            .Where(x => x.InvoiceId == invoice.Id &&
                        x.TenantId == tenantId &&
                        x.ProductId != null)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            var stock = await _db.ProductStockSnapshots
                .FirstOrDefaultAsync(s =>
                    s.ProductId == item.ProductId &&
                    s.TenantId == tenantId &&
                    s.BranchId == branchId,
                    ct);

            if (stock == null)
                continue;

            stock.QtyOnHand += item.Qty;
            stock.UpdatedAt = DateTime.UtcNow;

            _db.InventoryMoves.Add(new InventoryMove
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BranchId = branchId,
                ProductId = item.ProductId.Value,
                MoveType = "IN",
                Qty = item.Qty,
                Reason = "Refund Approved",
                RefType = "Refund",
                RefId = refund.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        // تحديث حالة الفاتورة
        var totalPaid = await _db.Payments
            .Where(p => p.InvoiceId == invoice.Id &&
                        p.TenantId == tenantId)
            .SumAsync(p => (int?)p.AmountCents, ct) ?? 0;

        var totalRefunded = await _db.Refunds
            .Where(r => r.InvoiceId == invoice.Id &&
                        r.TenantId == tenantId)
            .SumAsync(r => (int?)r.AmountCents, ct) ?? 0;

        var newPaid = totalPaid - totalRefunded;

        invoice.Status = newPaid == 0
            ? "Posted"
            : "PartiallyPaid";

        // تحديث الموافقة
        approval.ApprovedAt = DateTime.UtcNow;
        approval.ApprovedByUserId = Guid.Empty; // لاحقاً نربطه بالمستخدم

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new
        {
            Message = "Refund approved successfully.",
            refund.Id,
            invoice.Status
        });
    }
}