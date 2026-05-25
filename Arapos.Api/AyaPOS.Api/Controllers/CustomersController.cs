using Ayapos.Api.Contracts.Common;
using Ayapos.Api.Contracts.Customers;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/customers")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class CustomersController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public CustomersController(AyaposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListItemDto>>> List(
        [FromQuery] string? q,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null)
            return Unauthorized("Missing tenant.");

        var tenantId = _tenant.TenantId.Value;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        var tenantCustomerIds =
            _db.Customers.AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.Id)
            .Union(
            _db.Invoices.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.CustomerId != null)
                .Select(x => x.CustomerId!.Value)
            .Union(
                _db.CustomerLicenses.AsNoTracking()
                    .Where(x => x.TenantId == tenantId)
                    .Select(x => x.CustomerId)));

        var query =
            from c in _db.Customers.AsNoTracking()
            where tenantCustomerIds.Contains(c.Id)
            select new CustomerListItemDto
            {
                Id = c.Id,
                FullName = EF.Property<string?>(c, nameof(Customer.FullName)) ?? "Unnamed customer",
                Phone = EF.Property<string?>(c, nameof(Customer.Phone)),
                Email = EF.Property<string?>(c, nameof(Customer.Email)),
                Notes = EF.Property<string?>(c, nameof(Customer.Notes)),
                IsActive = EF.Property<bool?>(c, nameof(Customer.IsActive)) ?? true,
                CreatedAt = EF.Property<DateTime?>(c, nameof(Customer.CreatedAt)) ?? DateTime.UtcNow,
                InvoiceCount = _db.Invoices.AsNoTracking()
                    .Where(i => i.TenantId == tenantId && i.CustomerId == c.Id)
                    .Count(),
                LastInvoiceAt = _db.Invoices.AsNoTracking()
                    .Where(i => i.TenantId == tenantId && i.CustomerId == c.Id)
                    .Select(i => (DateTime?)i.CreatedAt)
                    .Max()
            };

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.FullName ?? "", $"%{term}%") ||
                (x.Phone != null && EF.Functions.Like(x.Phone, $"%{term}%")) ||
                (x.Email != null && EF.Functions.Like(x.Email, $"%{term}%")));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.FullName)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new PagedResult<CustomerListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        if (_tenant.TenantId is null)
            return Unauthorized("Missing tenant.");

        var tenantId = _tenant.TenantId.Value;
        var fullName = request.FullName.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest("Full name is required.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user context.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };

        _db.Customers.Add(customer);
        _db.CustomerLicenses.Add(new CustomerLicense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customer.Id,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddYears(100),
            Status = "ACTIVE",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return Ok(customer.Id);
    }

    [HttpPost("{customerId:guid}")]
    public async Task<ActionResult<CustomerListItemDto>> Update(
        Guid customerId,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken ct)
    {
        if (_tenant.TenantId is null)
            return Unauthorized("Missing tenant.");

        var tenantId = _tenant.TenantId.Value;

        var customerAssignedToTenant = await _db.CustomerLicenses
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.CustomerId == customerId, ct);

        if (!customerAssignedToTenant)
            return NotFound("Customer not found.");

        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == customerId, ct);
        if (customer is null)
            return NotFound("Customer not found.");

        var fullName = request.FullName.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest("Full name is required.");

        customer.FullName = fullName;
        customer.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        customer.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        customer.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        customer.IsActive = request.IsActive;
        customer.TenantId ??= tenantId;

        await _db.SaveChangesAsync(ct);

        var invoiceInfo = await _db.Invoices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CustomerId == customerId)
            .GroupBy(x => x.CustomerId!.Value)
            .Select(group => new
            {
                InvoiceCount = group.Count(),
                LastInvoiceAt = group.Max(x => (DateTime?)x.CreatedAt)
            })
            .FirstOrDefaultAsync(ct);

        return Ok(new CustomerListItemDto
        {
            Id = customer.Id,
            FullName = customer.FullName ?? "Unnamed customer",
            Phone = customer.Phone,
            Email = customer.Email,
            Notes = customer.Notes,
            IsActive = customer.IsActive,
            InvoiceCount = invoiceInfo?.InvoiceCount ?? 0,
            LastInvoiceAt = invoiceInfo?.LastInvoiceAt,
            CreatedAt = customer.CreatedAt
        });
    }
}
