using Arapos.Api.Contracts.Invoices;
using Arapos.Api.Data;
using Arapos.Api.Data.Entities;
using Arapos.Api.Security;
using Arapos.Api.Tenancy;
using Arapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/invoices")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class InvoicesController : ControllerBase
{
    private readonly AraposDbContext _db;
    private readonly ITenantContext _tenant;

    public InvoicesController(AraposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================================================
    // 1️⃣ Get Invoice
    // =========================================================
    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> GetInvoice(Guid invoiceId, CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant/branch context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i =>
                i.Id == invoiceId &&
                i.TenantId == tenantId &&
                i.BranchId == branchId, ct);

        if (invoice is null)
            return NotFound();

        var items = await _db.InvoiceItems
            .Where(x => x.InvoiceId == invoiceId &&
                        x.TenantId == tenantId)
            .ToListAsync(ct);

        return Ok(new
        {
            invoice.Id,
            invoice.InvoiceCode,
            invoice.Status,
            invoice.TotalCents,
            Items = items
        });
    }

    // =========================================================
    // 2️⃣ Create Draft
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant/branch context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var lastNo = await _db.Invoices
            .Where(i => i.TenantId == tenantId &&
                        i.BranchId == branchId)
            .MaxAsync(i => (int?)i.InvoiceNo, ct) ?? 0;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            InvoiceNo = lastNo + 1,
            InvoiceCode = $"INV-{(lastNo + 1):D6}",
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return Ok(invoice.Id);
    }

    // =========================================================
    // 3️⃣ Add Item
    // =========================================================
    [HttpPost("{invoiceId:guid}/items")]
    public async Task<IActionResult> AddItem(
        Guid invoiceId,
        [FromBody] AddInvoiceLineRequest request,
        CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant/branch context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(x =>
                x.Id == invoiceId &&
                x.TenantId == tenantId &&
                x.BranchId == branchId, ct);

        if (invoice is null)
            return NotFound();

        if (invoice.Status != "Draft")
            return BadRequest("Invoice not editable.");

        var lineTotal = request.PriceOverrideCents.GetValueOrDefault() * request.Qty;

        var item = new InvoiceItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoiceId,
            ItemType = request.ItemType,
            Qty = request.Qty,
            UnitPriceCents = request.PriceOverrideCents ?? 0,
            LineTotalCents = lineTotal
        };

        _db.InvoiceItems.Add(item);

        invoice.TotalCents += lineTotal;

        await _db.SaveChangesAsync(ct);

        return Ok(item.Id);
    }

    // =========================================================
    // 4️⃣ Finalize
    // =========================================================
    [HttpPost("{invoiceId:guid}/finalize")]
    public async Task<IActionResult> FinalizeInvoice(Guid invoiceId, CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice is null)
            return NotFound();

        if (invoice.Status != "Draft")
            return BadRequest("Already finalized.");

        if (invoice.TotalCents <= 0)
            return BadRequest("Empty invoice.");

        invoice.Status = "Posted";

        await _db.SaveChangesAsync(ct);

        return Ok(invoice.Status);
    }

    // =========================================================
    // 5️⃣ Add Payment + Stock Deduction
    // =========================================================
    [HttpPost("{invoiceId:guid}/payments")]
    public async Task<IActionResult> AddPayment(
        Guid invoiceId,
        [FromBody] AddPaymentRequest request,
        CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId &&
                                      i.TenantId == tenantId &&
                                      i.BranchId == branchId, ct);

        if (invoice is null)
            return NotFound();

        if (invoice.Status != "Posted")
            return BadRequest("Invoice not ready for payment.");

        var totalPaid = await _db.Payments
            .Where(p => p.InvoiceId == invoiceId &&
                        p.TenantId == tenantId)
            .SumAsync(p => (int?)p.AmountCents, ct) ?? 0;

        var remaining = invoice.TotalCents - totalPaid;

        if (request.AmountCents > remaining)
            return BadRequest("Payment exceeds remaining.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoiceId,
            Method = request.Method,
            AmountCents = request.AmountCents,
            PaidAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);

        totalPaid += request.AmountCents;

        invoice.Status =
            totalPaid == invoice.TotalCents
            ? "Paid"
            : "PartiallyPaid";

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            invoice.Status,
            TotalPaid = totalPaid,
            Remaining = invoice.TotalCents - totalPaid
        });
    }

    // =========================================================
    // 6️⃣ Refund (Request Only – Needs Manager Approval)
    // =========================================================
    [HttpPost("{invoiceId:guid}/refunds")]
    public async Task<IActionResult> Refund(
        Guid invoiceId,
        [FromBody] int amountCents,
        CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId &&
                                      i.TenantId == tenantId &&
                                      i.BranchId == branchId, ct);

        if (invoice is null)
            return NotFound();

        if (invoice.Status != "Paid" && invoice.Status != "PartiallyPaid")
            return BadRequest("Invoice not refundable.");

        var approval = new ManagerApproval
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            ApprovalType = "Refund",
            RefType = "Invoice",
            RefId = invoiceId,
            Notes = $"Refund request for {amountCents} cents"
        };

        _db.ManagerApprovals.Add(approval);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            Message = "Refund request sent for approval.",
            approval.Id
        });
    }
}