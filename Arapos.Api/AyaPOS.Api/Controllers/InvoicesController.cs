using Ayapos.Api.Contracts.Common;
using Ayapos.Api.Contracts.Invoices;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/invoices")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class InvoicesController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public InvoicesController(AyaposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> List(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant/branch context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        var query = _db.Invoices
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.BranchId == branchId)
            .Select(i => new
            {
                i.Id,
                InvoiceCode = EF.Property<string?>(i, nameof(Invoice.InvoiceCode)) ?? string.Empty,
                Status = EF.Property<string?>(i, nameof(Invoice.Status)) ?? "DRAFT",
                CustomerId = EF.Property<Guid?>(i, nameof(Invoice.CustomerId)),
                TotalCents = EF.Property<int?>(i, nameof(Invoice.TotalCents)) ?? 0,
                CreatedAt = EF.Property<DateTime?>(i, nameof(Invoice.CreatedAt)) ?? DateTime.UtcNow
            })
            .Select(i => new InvoiceListItemDto
            {
                Id = i.Id,
                InvoiceCode = i.InvoiceCode,
                Status = i.Status,
                CustomerName = i.CustomerId == null
                    ? string.Empty
                    : _db.Customers
                        .AsNoTracking()
                        .Where(c => c.Id == i.CustomerId.Value)
                        .Select(c => EF.Property<string?>(c, nameof(Customer.FullName)) ?? string.Empty)
                        .FirstOrDefault() ?? string.Empty,
                Total = i.TotalCents / 100m,
                TotalPaid = (_db.Payments
                        .AsNoTracking()
                        .Where(p => p.TenantId == tenantId && p.InvoiceId == i.Id)
                        .Sum(p => (int?)p.AmountCents) ?? 0) / 100m,
                Remaining = (i.TotalCents - (
                        _db.Payments
                            .AsNoTracking()
                            .Where(p => p.TenantId == tenantId && p.InvoiceId == i.Id)
                            .Sum(p => (int?)p.AmountCents) ?? 0)) / 100m,
                CreatedAt = i.CreatedAt,
                AppointmentId = EF.Property<Guid?>(i, nameof(Invoice.AppointmentId))
            });

        if (dateFrom.HasValue)
        {
            var from = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt >= from);
        }

        if (dateTo.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.InvoiceCode, $"%{term}%") ||
                EF.Functions.Like(x.CustomerName ?? string.Empty, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new PagedResult<InvoiceListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    // =========================================================
    // 1️⃣ Get Invoice
    // =========================================================
    [HttpGet("{invoiceId:guid}")]
    public async Task<ActionResult<InvoiceDetailsDto>> GetInvoice(Guid invoiceId, CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant/branch context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var invoice = await (
            from i in _db.Invoices.AsNoTracking()
            join c in _db.Customers.AsNoTracking() on EF.Property<Guid?>(i, nameof(Invoice.CustomerId)) equals c.Id into customerGroup
            from customer in customerGroup.DefaultIfEmpty()
            where i.Id == invoiceId &&
                  i.TenantId == tenantId &&
                  i.BranchId == branchId
            select new
            {
                Id = i.Id,
                InvoiceCode = EF.Property<string?>(i, nameof(Invoice.InvoiceCode)) ?? string.Empty,
                Status = EF.Property<string?>(i, nameof(Invoice.Status)) ?? "DRAFT",
                CustomerId = EF.Property<Guid?>(i, nameof(Invoice.CustomerId)),
                SubtotalCents = EF.Property<int?>(i, nameof(Invoice.SubtotalCents)) ?? 0,
                TotalCents = EF.Property<int?>(i, nameof(Invoice.TotalCents)) ?? 0,
                CreatedAt = EF.Property<DateTime?>(i, nameof(Invoice.CreatedAt)) ?? DateTime.UtcNow,
                CustomerName = customer != null ? EF.Property<string?>(customer, nameof(Customer.FullName)) : null
            })
            .FirstOrDefaultAsync(ct);

        if (invoice is null)
            return NotFound();

        var items = await _db.InvoiceItems
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId &&
                        x.TenantId == tenantId)
            .OrderBy(x => x.Id)
            .Select(x => new InvoiceLineDto
            {
                Id = x.Id,
                ItemType = x.ItemType,
                Name = x.NameSnapshot,
                Qty = x.Qty,
                UnitPrice = x.UnitPriceCents / 100m,
                LineTotal = x.LineTotalCents / 100m,
                CurrencyCode = x.CurrencyCode
            })
            .ToListAsync(ct);

        var totalPaidCents = await _db.Payments
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId && x.TenantId == tenantId)
            .SumAsync(x => (int?)x.AmountCents, ct) ?? 0;

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId && x.TenantId == tenantId)
            .OrderByDescending(x => x.PaidAt)
            .Select(x => new InvoicePaymentDto
            {
                Id = x.Id,
                Method = x.Method.ToString(),
                Amount = x.AmountCents / 100m,
                Reference = x.Reference,
                PaidAt = x.PaidAt
            })
            .ToListAsync(ct);

        var printSettings = await _db.BranchSettings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.BranchId == branchId)
            .Select(x => new InvoicePrintSettingsDto
            {
                CompanyName = x.CompanyName,
                CompanyLogoUrl = x.CompanyLogoUrl,
                CompanyPhone = x.CompanyPhone,
                CompanyAddress = x.CompanyAddress,
                CompanyTaxNumber = x.CompanyTaxNumber,
                ReceiptTitle = string.IsNullOrWhiteSpace(x.ReceiptTitle) ? "Sales Receipt" : x.ReceiptTitle,
                ReceiptHeaderLine1 = x.ReceiptHeaderLine1,
                ReceiptHeaderLine2 = x.ReceiptHeaderLine2,
                ReceiptFooterNote = x.ReceiptFooterNote,
                ShowBranchNameOnReceipt = x.ShowBranchNameOnReceipt,
                ShowCustomerNameOnReceipt = x.ShowCustomerNameOnReceipt,
                ShowPaymentHistoryOnReceipt = x.ShowPaymentHistoryOnReceipt,
                AutoPrintReceiptAfterPayment = x.AutoPrintReceiptAfterPayment
            })
            .FirstOrDefaultAsync(ct)
            ?? new InvoicePrintSettingsDto();

        return Ok(new InvoiceDetailsDto
        {
            Id = invoice.Id,
            InvoiceCode = invoice.InvoiceCode,
            Status = invoice.Status,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            Subtotal = invoice.SubtotalCents / 100m,
            Total = invoice.TotalCents / 100m,
            TotalPaid = totalPaidCents / 100m,
            Remaining = (invoice.TotalCents - totalPaidCents) / 100m,
            CreatedAt = invoice.CreatedAt,
            PrintSettings = printSettings,
            Items = items,
            Payments = payments
        });
    }

    // =========================================================
    // 2️⃣ Create Draft
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest? request, CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant/branch context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        if (request?.CustomerId is not null)
        {
            var customerAssignedToTenant = await _db.CustomerLicenses
                .AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.CustomerId == request.CustomerId.Value, ct);

            if (!customerAssignedToTenant)
                return BadRequest("Customer is not assigned to this tenant.");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user context.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.UtcNow;

        var sequence = await _db.InvoiceSequences
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.BranchId == branchId, ct);

        var nextNumberFromInvoices = ((await _db.Invoices
            .Where(i => i.TenantId == tenantId && i.BranchId == branchId)
            .MaxAsync(i => (int?)i.InvoiceNo, ct) ?? 0) + 1);

        var invoiceNo = Math.Max(sequence?.NextNumber ?? nextNumberFromInvoices, nextNumberFromInvoices);

        if (sequence is null)
        {
            _db.InvoiceSequences.Add(new InvoiceSequence
            {
                TenantId = tenantId,
                BranchId = branchId,
                NextNumber = invoiceNo + 1,
                UpdatedAt = now
            });
        }
        else
        {
            sequence.NextNumber = invoiceNo + 1;
            sequence.UpdatedAt = now;
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            InvoiceNo = invoiceNo,
            InvoiceCode = $"INV-{invoiceNo:D6}",
            CustomerId = request?.CustomerId,
            Status = "Draft",
            CreatedByUserId = userId,
            CreatedAt = now
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Ok(invoice.Id);
    }

    [HttpPost("{invoiceId:guid}/customer")]
    public async Task<IActionResult> SetCustomer(
        Guid invoiceId,
        [FromBody] SetInvoiceCustomerRequest request,
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
            return NotFound("Invoice not found.");

        if (invoice.Status != "Draft")
            return BadRequest("Customer can only be changed while the invoice is still a draft.");

        if (request.CustomerId is not null)
        {
            var customerAssignedToTenant = await _db.CustomerLicenses
                .AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.CustomerId == request.CustomerId.Value, ct);

            if (!customerAssignedToTenant)
                return BadRequest("Customer is not assigned to this tenant.");
        }

        invoice.CustomerId = request.CustomerId;
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

        if (request.Qty <= 0)
            return BadRequest("Qty must be greater than zero.");

        var itemType = request.ItemType.Trim();
        if (!string.Equals(itemType, "Product", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(itemType, "Service", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported item type.");
        }

        string nameSnapshot;
        string currencyCode;
        Guid? productId = null;
        Guid? serviceId = null;
        int basePriceCents;

        if (string.Equals(itemType, "Product", StringComparison.OrdinalIgnoreCase))
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.ItemId &&
                    x.TenantId == tenantId &&
                    x.BranchId == branchId, ct);

            if (product is null)
                return BadRequest("Product not found.");

            nameSnapshot = product.NameEn ?? product.NameAr ?? product.Sku ?? "Product";
            currencyCode = product.CurrencyCode ?? "AED";
            basePriceCents = request.PriceOverrideCents ?? (int)Math.Round(product.SellPrice * 100m);
            productId = product.Id;
            itemType = "Product";
        }
        else
        {
            var serviceData = await (
                from s in _db.Services.AsNoTracking()
                join sp in _db.ServicePrices.AsNoTracking() on s.Id equals sp.ServiceId
                where s.Id == request.ItemId
                   && s.TenantId == tenantId
                   && s.BranchId == branchId
                   && sp.TenantId == tenantId
                   && sp.BranchId == branchId
                   && sp.IsActive
                select new
                {
                    s.Id,
                    s.NameAr,
                    s.NameEn,
                    sp.CurrencyCode,
                    sp.PriceCents
                })
                .FirstOrDefaultAsync(ct);

            if (serviceData is null)
                return BadRequest("Service not found.");

            nameSnapshot = serviceData.NameEn ?? serviceData.NameAr ?? "Service";
            currencyCode = serviceData.CurrencyCode;
            basePriceCents = request.PriceOverrideCents ?? serviceData.PriceCents;
            serviceId = serviceData.Id;
            itemType = "Service";
        }

        if (basePriceCents < 0)
            return BadRequest("Price must not be negative.");

        var lineTotal = basePriceCents * request.Qty;

        var item = new InvoiceItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoiceId,
            ItemType = itemType,
            NameSnapshot = nameSnapshot,
            Qty = request.Qty,
            UnitPriceCents = basePriceCents,
            LineTotalCents = lineTotal
            ,
            CurrencyCode = currencyCode,
            ProductId = productId,
            ServiceId = serviceId,
            PriceOverrideCents = request.PriceOverrideCents,
            PriceOverrideReason = request.PriceOverrideReason
        };

        _db.InvoiceItems.Add(item);

        invoice.SubtotalCents += lineTotal;
        invoice.TotalCents += lineTotal;

        await _db.SaveChangesAsync(ct);

        return Ok(item.Id);
    }

    [HttpPost("{invoiceId:guid}/items/{lineId:guid}")]
    public async Task<IActionResult> UpdateItem(
        Guid invoiceId,
        Guid lineId,
        [FromBody] UpdateInvoiceLineRequest request,
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
            return NotFound("Invoice not found.");

        if (invoice.Status != "Draft")
            return BadRequest("Invoice not editable.");

        var line = await _db.InvoiceItems
            .FirstOrDefaultAsync(x =>
                x.Id == lineId &&
                x.InvoiceId == invoiceId &&
                x.TenantId == tenantId, ct);

        if (line is null)
            return NotFound("Invoice line not found.");

        if (request.Qty <= 0)
            return BadRequest("Qty must be greater than zero.");

        var nextUnitPrice = request.PriceOverrideCents ?? line.UnitPriceCents;
        if (nextUnitPrice < 0)
            return BadRequest("Price must not be negative.");

        invoice.SubtotalCents -= line.LineTotalCents;
        invoice.TotalCents -= line.LineTotalCents;

        line.Qty = request.Qty;
        line.UnitPriceCents = nextUnitPrice;
        line.LineTotalCents = nextUnitPrice * request.Qty;
        line.PriceOverrideCents = request.PriceOverrideCents;
        line.PriceOverrideReason = request.PriceOverrideReason;

        invoice.SubtotalCents += line.LineTotalCents;
        invoice.TotalCents += line.LineTotalCents;

        await _db.SaveChangesAsync(ct);
        return Ok(line.Id);
    }

    [HttpDelete("{invoiceId:guid}/items/{lineId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid invoiceId, Guid lineId, CancellationToken ct)
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
            return NotFound("Invoice not found.");

        if (invoice.Status != "Draft")
            return BadRequest("Invoice not editable.");

        var line = await _db.InvoiceItems
            .FirstOrDefaultAsync(x =>
                x.Id == lineId &&
                x.InvoiceId == invoiceId &&
                x.TenantId == tenantId, ct);

        if (line is null)
            return NotFound("Invoice line not found.");

        invoice.SubtotalCents -= line.LineTotalCents;
        invoice.TotalCents -= line.LineTotalCents;
        if (invoice.SubtotalCents < 0) invoice.SubtotalCents = 0;
        if (invoice.TotalCents < 0) invoice.TotalCents = 0;

        _db.InvoiceItems.Remove(line);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // =========================================================
    // 4️⃣ Finalize
    // =========================================================
    [HttpPost("{invoiceId:guid}/finalize")]
    public async Task<IActionResult> FinalizeInvoice(Guid invoiceId, CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i =>
                i.Id == invoiceId &&
                i.TenantId == tenantId &&
                i.BranchId == branchId, ct);

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

        if (request.AmountCents <= 0)
            return BadRequest("Payment amount must be greater than zero.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId &&
                                      i.TenantId == tenantId &&
                                      i.BranchId == branchId, ct);

        if (invoice is null)
            return NotFound();

        var openSessionExists = await _db.CashierSessions
            .AsNoTracking()
            .AnyAsync(session =>
                session.TenantId == tenantId &&
                session.BranchId == branchId &&
                !session.IsClosed, ct);

        if (!openSessionExists)
            return BadRequest("Open a cashier session before collecting payments for this branch.");

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

        if (request.AppointmentId is not null)
        {
            var appointment = await _db.Appointments
                .FirstOrDefaultAsync(x =>
                    x.Id == request.AppointmentId.Value &&
                    x.TenantId == tenantId &&
                    x.BranchId == branchId &&
                    x.CustomerId == invoice.CustomerId, ct);

            if (appointment is not null && appointment.Status is not "cancelled" and not "no_show")
            {
                appointment.Status = invoice.Status == "Paid" ? "completed" : "checked_in";
                // Persist the appointment link on the invoice for reporting
                invoice.AppointmentId = appointment.Id;
            }
        }
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

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


