using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/approvals")]
[Authorize(Policy = AuthPolicies.TenantAdmin)]
[RequireBranchHeader]
public class ApprovalsController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public ApprovalsController(AyaposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

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
                a.BranchId == branchId &&
                a.ApprovalType == "Refund",
                ct);

        if (approval is null)
            return NotFound("Approval not found.");

        if (approval.ApprovedAt > DateTime.MinValue)
            return BadRequest("Already approved.");

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i =>
                i.Id == approval.RefId &&
                i.TenantId == tenantId &&
                i.BranchId == branchId,
                ct);

        if (invoice is null)
            return NotFound("Invoice not found.");

        if (string.IsNullOrWhiteSpace(approval.Notes))
            return BadRequest("Missing refund details.");

        var amountText = approval.Notes.Replace("Refund request for ", "").Replace(" cents", "");
        if (!int.TryParse(amountText, out var amountCents))
            return BadRequest("Invalid refund amount.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoice.Id,
            PaymentId = Guid.Empty,
            AmountCents = amountCents,
            Reason = "Refund Approved",
            RefundedAt = DateTime.UtcNow
        };

        _db.Refunds.Add(refund);

        var items = await _db.InvoiceItems
            .Where(x => x.InvoiceId == invoice.Id &&
                        x.TenantId == tenantId &&
                        x.ProductId != null)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            var productId = item.ProductId!.Value;

            var stock = await _db.ProductStockSnapshots
                .FirstOrDefaultAsync(s =>
                    s.ProductId == productId &&
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
                ProductId = productId,
                MoveType = "IN",
                Qty = item.Qty,
                Reason = "Refund Approved",
                RefType = "Refund",
                RefId = refund.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        var totalPaid = await _db.Payments
            .Where(p => p.InvoiceId == invoice.Id &&
                        p.TenantId == tenantId)
            .SumAsync(p => (int?)p.AmountCents, ct) ?? 0;

        var totalRefundedBeforeCurrent = await _db.Refunds
            .Where(r => r.InvoiceId == invoice.Id &&
                        r.TenantId == tenantId &&
                        r.Id != refund.Id)
            .SumAsync(r => (int?)r.AmountCents, ct) ?? 0;

        var newPaid = totalPaid - (totalRefundedBeforeCurrent + amountCents);

        invoice.Status = newPaid <= 0
            ? "Posted"
            : "PartiallyPaid";

        approval.ApprovedAt = DateTime.UtcNow;
        approval.ApprovedByUserId = Guid.TryParse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value, out var userId)
            ? userId
            : Guid.Empty;

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
