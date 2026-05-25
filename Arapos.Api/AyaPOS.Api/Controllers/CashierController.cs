using Ayapos.Api.Contracts.Cashier;
using Ayapos.Api.Contracts.Common;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/cashier")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public class CashierController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public CashierController(AyaposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("current")]
    public async Task<ActionResult<CashierSessionDto?>> GetCurrentSession(CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var session = await _db.CashierSessions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == _tenant.TenantId.Value &&
                x.BranchId == _tenant.BranchId.Value &&
                !x.IsClosed)
            .OrderByDescending(x => x.OpenedAt)
            .FirstOrDefaultAsync(ct);

        if (session is null)
            return Ok(null);

        return Ok(await BuildSessionDtoAsync(session, ct));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<PagedResult<CashierSessionDto>>> ListSessions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.CashierSessions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == _tenant.TenantId.Value &&
                x.BranchId == _tenant.BranchId.Value);

        var total = await query.CountAsync(ct);
        var sessions = await query
            .OrderByDescending(x => x.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = new List<CashierSessionDto>(sessions.Count);
        foreach (var session in sessions)
            items.Add(await BuildSessionDtoAsync(session, ct));

        return Ok(new PagedResult<CashierSessionDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("daily-summary")]
    public async Task<ActionResult<CashierDailySummaryDto>> GetDailySummary(CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;
        var startOfDayUtc = DateTime.UtcNow.Date;
        var endOfDayUtc = startOfDayUtc.AddDays(1);

        var invoicesQuery = _db.Invoices
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.BranchId == branchId &&
                x.CreatedAt >= startOfDayUtc &&
                x.CreatedAt < endOfDayUtc);

        var salesInvoicesQuery = invoicesQuery
            .Where(x => x.Status == "Posted" || x.Status == "PartiallyPaid" || x.Status == "Paid");

        var invoiceCount = await invoicesQuery.CountAsync(ct);
        var postedInvoiceCount = await salesInvoicesQuery.CountAsync(ct);
        var paidInvoiceCount = await salesInvoicesQuery.CountAsync(x => x.Status == "Paid", ct);
        var grossSalesCents = await salesInvoicesQuery.SumAsync(x => (int?)x.TotalCents, ct) ?? 0;

        var collectedCents = await (
            from payment in _db.Payments.AsNoTracking()
            join invoice in _db.Invoices.AsNoTracking() on payment.InvoiceId equals invoice.Id
            where payment.TenantId == tenantId
               && invoice.BranchId == branchId
               && payment.PaidAt >= startOfDayUtc
               && payment.PaidAt < endOfDayUtc
            select payment.AmountCents)
            .SumAsync(ct);

        var activeCustomerCount = await (
            from customer in _db.Customers.AsNoTracking()
            join invoice in _db.Invoices.AsNoTracking() on customer.Id equals invoice.CustomerId
            where invoice.TenantId == tenantId
               && invoice.BranchId == branchId
               && invoice.CreatedAt >= startOfDayUtc
               && invoice.CreatedAt < endOfDayUtc
               && (invoice.Status == "Posted" || invoice.Status == "PartiallyPaid" || invoice.Status == "Paid")
               && customer.IsActive
            select customer.Id)
            .Distinct()
            .CountAsync(ct);

        var topProducts = await (
            from item in _db.InvoiceItems.AsNoTracking()
            join invoice in _db.Invoices.AsNoTracking() on item.InvoiceId equals invoice.Id
            where invoice.TenantId == tenantId
               && invoice.BranchId == branchId
               && invoice.CreatedAt >= startOfDayUtc
               && invoice.CreatedAt < endOfDayUtc
               && (invoice.Status == "Posted" || invoice.Status == "PartiallyPaid" || invoice.Status == "Paid")
               && item.ItemType == "Product"
            group item by new { item.NameSnapshot, item.CurrencyCode } into grouped
            orderby grouped.Sum(x => x.LineTotalCents) descending, grouped.Sum(x => x.Qty) descending
            select new CashierTopItemDto
            {
                ItemType = "Product",
                Name = grouped.Key.NameSnapshot,
                Quantity = grouped.Sum(x => x.Qty),
                TotalCents = grouped.Sum(x => x.LineTotalCents),
                CurrencyCode = grouped.Key.CurrencyCode
            })
            .Take(5)
            .ToListAsync(ct);

        var topServices = await (
            from item in _db.InvoiceItems.AsNoTracking()
            join invoice in _db.Invoices.AsNoTracking() on item.InvoiceId equals invoice.Id
            where invoice.TenantId == tenantId
               && invoice.BranchId == branchId
               && invoice.CreatedAt >= startOfDayUtc
               && invoice.CreatedAt < endOfDayUtc
               && (invoice.Status == "Posted" || invoice.Status == "PartiallyPaid" || invoice.Status == "Paid")
               && item.ItemType == "Service"
            group item by new { item.NameSnapshot, item.CurrencyCode } into grouped
            orderby grouped.Sum(x => x.LineTotalCents) descending, grouped.Sum(x => x.Qty) descending
            select new CashierTopItemDto
            {
                ItemType = "Service",
                Name = grouped.Key.NameSnapshot,
                Quantity = grouped.Sum(x => x.Qty),
                TotalCents = grouped.Sum(x => x.LineTotalCents),
                CurrencyCode = grouped.Key.CurrencyCode
            })
            .Take(5)
            .ToListAsync(ct);

        var recentPayments = await (
            from payment in _db.Payments.AsNoTracking()
            join invoice in _db.Invoices.AsNoTracking() on payment.InvoiceId equals invoice.Id
            where payment.TenantId == tenantId
               && invoice.BranchId == branchId
               && payment.PaidAt >= startOfDayUtc
               && payment.PaidAt < endOfDayUtc
            orderby payment.PaidAt descending
            select new CashierRecentPaymentDto
            {
                PaymentId = payment.Id,
                InvoiceCode = invoice.InvoiceCode,
                Method = payment.Method.ToString(),
                AmountCents = payment.AmountCents,
                Reference = payment.Reference,
                PaidAt = payment.PaidAt
            })
            .Take(6)
            .ToListAsync(ct);

        return Ok(new CashierDailySummaryDto
        {
            BusinessDateUtc = startOfDayUtc,
            InvoiceCount = invoiceCount,
            PostedInvoiceCount = postedInvoiceCount,
            PaidInvoiceCount = paidInvoiceCount,
            ActiveCustomerCount = activeCustomerCount,
            GrossSalesCents = grossSalesCents,
            CollectedCents = collectedCents,
            RemainingCents = grossSalesCents - collectedCents,
            TopProducts = topProducts,
            TopServices = topServices,
            RecentPayments = recentPayments
        });
    }

    [HttpPost("open")]
    public async Task<IActionResult> OpenSession([FromBody] OpenCashierSessionRequest request, CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user context.");

        var alreadyOpen = await _db.CashierSessions
            .AnyAsync(x =>
                x.TenantId == _tenant.TenantId.Value &&
                x.BranchId == _tenant.BranchId.Value &&
                !x.IsClosed, ct);

        if (alreadyOpen)
            return BadRequest("There is already an open cashier session for this branch.");

        var session = new CashierSession
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            BranchId = _tenant.BranchId.Value,
            UserId = userId,
            OpenedAt = DateTime.UtcNow,
            OpeningCashCents = request.OpeningCashCents,
            IsClosed = false
        };

        _db.CashierSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return Ok(session.Id);
    }

    [HttpPost("{sessionId:guid}/close")]
    public async Task<IActionResult> CloseSession(
        Guid sessionId,
        [FromBody] CloseCashierSessionRequest request,
        CancellationToken ct)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant context.");

        var session = await _db.CashierSessions
            .FirstOrDefaultAsync(x =>
                x.Id == sessionId &&
                x.TenantId == _tenant.TenantId.Value &&
                x.BranchId == _tenant.BranchId.Value, ct);

        if (session == null || session.IsClosed)
            return BadRequest("Invalid session.");

        var now = DateTime.UtcNow;

        var metrics = await CalculateSessionMetricsAsync(session.TenantId, session.BranchId, session.OpenedAt, now, ct);

        session.TotalCashCents = metrics.TotalCashCents;
        session.TotalCardCents = metrics.TotalCardCents;
        session.TotalTransferCents = metrics.TotalTransferCents;
        session.TotalRefundCents = metrics.TotalRefundCents;
        session.ExpectedCashCents = session.OpeningCashCents + session.TotalCashCents - session.TotalRefundCents;
        session.ActualCashCents = request.ActualCashCents;
        session.DifferenceCents = session.ActualCashCents - session.ExpectedCashCents;
        session.DiscrepancyReason = string.IsNullOrWhiteSpace(request.DiscrepancyReason) ? null : request.DiscrepancyReason.Trim();
        session.ClosedAt = now;
        session.IsClosed = true;

        await _db.SaveChangesAsync(ct);

        return Ok(await BuildSessionDtoAsync(session, ct));
    }

    private async Task<CashierSessionDto> BuildSessionDtoAsync(CashierSession session, CancellationToken ct)
    {
        var sessionEnd = session.ClosedAt ?? DateTime.UtcNow;
        var metrics = await CalculateSessionMetricsAsync(session.TenantId, session.BranchId, session.OpenedAt, sessionEnd, ct);
        var username = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == session.UserId)
            .Select(user => user.Username)
            .FirstOrDefaultAsync(ct) ?? "Unknown cashier";
        var invoices = await LoadSessionInvoicesAsync(session.TenantId, session.BranchId, session.OpenedAt, sessionEnd, ct);

        return new CashierSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Username = username,
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,
            OpeningCashCents = session.OpeningCashCents,
            TotalCashCents = metrics.TotalCashCents,
            TotalCardCents = metrics.TotalCardCents,
            TotalTransferCents = metrics.TotalTransferCents,
            TotalRefundCents = metrics.TotalRefundCents,
            ExpectedCashCents = session.OpeningCashCents + metrics.TotalCashCents - metrics.TotalRefundCents,
            ActualCashCents = session.ActualCashCents,
            DifferenceCents = session.IsClosed
                ? session.ActualCashCents - (session.OpeningCashCents + metrics.TotalCashCents - metrics.TotalRefundCents)
                : 0,
            DiscrepancyReason = session.DiscrepancyReason,
            SalesInvoiceCount = metrics.SalesInvoiceCount,
            GrossSalesCents = metrics.GrossSalesCents,
            CollectedCents = metrics.CollectedCents,
            IsClosed = session.IsClosed,
            Invoices = invoices
        };

    }

    private async Task<IReadOnlyList<CashierSessionInvoiceDto>> LoadSessionInvoicesAsync(
        Guid tenantId,
        Guid branchId,
        DateTime openedAt,
        DateTime sessionEnd,
        CancellationToken ct)
    {
        return await _db.Invoices
            .AsNoTracking()
            .Where(invoice =>
                invoice.TenantId == tenantId &&
                invoice.BranchId == branchId &&
                invoice.CreatedAt >= openedAt &&
                invoice.CreatedAt <= sessionEnd)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .Select(invoice => new CashierSessionInvoiceDto
            {
                Id = invoice.Id,
                InvoiceCode = invoice.InvoiceCode ?? string.Empty,
                Status = invoice.Status ?? "Draft",
                CustomerName = invoice.CustomerId == null
                    ? string.Empty
                    : _db.Customers
                        .AsNoTracking()
                        .Where(customer => customer.Id == invoice.CustomerId.Value)
                        .Select(customer => customer.FullName ?? string.Empty)
                        .FirstOrDefault() ?? string.Empty,
                TotalCents = invoice.TotalCents,
                CollectedCents = _db.Payments
                    .AsNoTracking()
                    .Where(payment => payment.TenantId == tenantId && payment.InvoiceId == invoice.Id)
                    .Sum(payment => (int?)payment.AmountCents) ?? 0,
                CreatedAt = invoice.CreatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<SessionMetrics> CalculateSessionMetricsAsync(
        Guid tenantId,
        Guid branchId,
        DateTime openedAt,
        DateTime sessionEnd,
        CancellationToken ct)
    {
        var saleStatuses = new[] { "Posted", "PartiallyPaid", "Paid" };

        var sessionInvoices = _db.Invoices
            .AsNoTracking()
            .Where(i =>
                i.TenantId == tenantId &&
                i.BranchId == branchId &&
                i.CreatedAt >= openedAt &&
                i.CreatedAt <= sessionEnd);

        var salesInvoiceCount = await sessionInvoices.CountAsync(i => saleStatuses.Contains(i.Status), ct);
        var grossSalesCents = await sessionInvoices
            .Where(i => saleStatuses.Contains(i.Status))
            .SumAsync(i => (int?)i.TotalCents, ct) ?? 0;

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p =>
                p.TenantId == tenantId &&
                p.PaidAt >= openedAt &&
                p.PaidAt <= sessionEnd &&
                _db.Invoices.Any(i =>
                    i.Id == p.InvoiceId &&
                    i.TenantId == tenantId &&
                    i.BranchId == branchId))
            .Select(p => new { p.AmountCents, p.Method })
            .ToListAsync(ct);

        var refunds = await _db.Refunds
            .AsNoTracking()
            .Where(r =>
                r.TenantId == tenantId &&
                r.RefundedAt >= openedAt &&
                r.RefundedAt <= sessionEnd &&
                _db.Invoices.Any(i =>
                    i.Id == r.InvoiceId &&
                    i.TenantId == tenantId &&
                    i.BranchId == branchId))
            .SumAsync(r => (int?)r.AmountCents, ct) ?? 0;

        return new SessionMetrics
        {
            SalesInvoiceCount = salesInvoiceCount,
            GrossSalesCents = grossSalesCents,
            CollectedCents = payments.Sum(p => p.AmountCents),
            TotalCashCents = payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.AmountCents),
            TotalCardCents = payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.AmountCents),
            TotalTransferCents = payments.Where(p => p.Method == PaymentMethod.BankTransfer).Sum(p => p.AmountCents),
            TotalRefundCents = refunds
        };
    }

    private sealed class SessionMetrics
    {
        public int SalesInvoiceCount { get; init; }
        public int GrossSalesCents { get; init; }
        public int CollectedCents { get; init; }
        public int TotalCashCents { get; init; }
        public int TotalCardCents { get; init; }
        public int TotalTransferCents { get; init; }
        public int TotalRefundCents { get; init; }
    }
}
