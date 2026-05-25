using Ayapos.Api.Contracts.Appointments;
using Ayapos.Api.Contracts.Common;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/appointments")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class AppointmentsController : ControllerBase
{
    private static readonly string[] AllowedStatuses = ["scheduled", "confirmed", "completed", "cancelled", "no_show"];
    private static readonly Regex ResourcePrefixRegex = new(@"^\[resource:(?<resource>[^\]]+)\]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public AppointmentsController(AyaposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("resources")]
    public async Task<ActionResult<IReadOnlyList<AppointmentResourceDto>>> ListResources(CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return BadRequest("Missing tenant/branch context.");

        var items = await _db.Staff
            .AsNoTracking()
            .Where(x =>
                x.TenantId == _tenant.TenantId.Value &&
                x.BranchId == _tenant.BranchId.Value &&
                x.IsActive &&
                x.IsBookableForAppointments)
            .Select(x => new AppointmentResourceDto
            {
                UserId = x.Id,
                Username = x.FullName,
                Role = x.JobTitle ?? "Staff"
            })
            .OrderBy(x => x.Role)
            .ThenBy(x => x.Username)
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AppointmentListItemDto>>> List(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant/branch context.");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;
        var tenantCustomerIds = _db.Customers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id);

        var query =
            from appointment in _db.Appointments.AsNoTracking()
            join customer in _db.Customers.AsNoTracking()
                on appointment.CustomerId equals customer.Id into customerGroup
            from customer in customerGroup.DefaultIfEmpty()
            join staff in _db.Staff.AsNoTracking()
                on appointment.StaffId equals staff.Id into staffGroup
            from staff in staffGroup.DefaultIfEmpty()
            where appointment.TenantId == tenantId
               && appointment.BranchId == branchId
               && appointment.CustomerId.HasValue
               && tenantCustomerIds.Contains(appointment.CustomerId ?? Guid.Empty)
            select new AppointmentListItemDto
            {
                Id = appointment.Id,
                CustomerId = appointment.CustomerId,
                ServiceId = appointment.AppointmentItems
                    .Where(item => item.ItemType == "service")
                    .Select(item => (Guid?)item.ItemId)
                    .FirstOrDefault(),
                CustomerName = customer != null ? customer.FullName ?? "Unnamed customer" : string.Empty,
                CustomerPhone = customer != null ? customer.Phone ?? string.Empty : string.Empty,
                ServiceName = appointment.AppointmentItems
                    .Where(item => item.ItemType == "service")
                    .Select(item => item.Name)
                    .FirstOrDefault() ?? string.Empty,
                ServicePrice = appointment.AppointmentItems
                    .Where(item => item.ItemType == "service")
                    .Select(item => (decimal?)item.UnitPrice)
                    .FirstOrDefault(),
                CurrencyCode = appointment.AppointmentItems
                    .Where(item => item.ItemType == "service")
                    .Select(item => item.CurrencyCode)
                    .FirstOrDefault() ?? "AED",
                ResourceName = staff != null ? staff.FullName : string.Empty,
                StartAt = appointment.StartAt,
                EndAt = appointment.EndAt,
                Status = appointment.Status,
                Notes = appointment.Notes ?? string.Empty,
                ItemCount = appointment.AppointmentItems.Count,
                CreatedAt = appointment.CreatedAt
            };

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.ToLower() == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.CustomerName, $"%{term}%") ||
                EF.Functions.Like(x.CustomerPhone, $"%{term}%") ||
                EF.Functions.Like(x.Notes, $"%{term}%") ||
                EF.Functions.Like(x.Status, $"%{term}%"));
        }

        if (dateFrom.HasValue)
        {
            var fromDate = dateFrom.Value;
            query = query.Where(x => x.StartAt >= fromDate);
        }

        if (dateTo.HasValue)
        {
            var toDate = dateTo.Value;
            query = query.Where(x => x.StartAt < toDate);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.StartAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            item.ResourceName = string.IsNullOrWhiteSpace(item.ResourceName) ? ExtractResourceName(item.Notes) : item.ResourceName;
            item.Notes = StripResourcePrefix(item.Notes);
        }

        return Ok(new PagedResult<AppointmentListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("schedule")]
    public async Task<ActionResult<AppointmentScheduleBoardDto>> GetSchedule(
        [FromQuery] DateTime? date,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant/branch context.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;
        var scheduleDate = (date ?? DateTime.UtcNow).Date;
        var nextDate = scheduleDate.AddDays(1);

        var resources = await _db.Staff
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.BranchId == branchId &&
                x.IsActive &&
                x.IsBookableForAppointments)
            .Select(x => new AppointmentResourceDto
            {
                UserId = x.Id,
                Username = x.FullName,
                Role = x.JobTitle ?? "Staff"
            })
            .OrderBy(x => x.Role)
            .ThenBy(x => x.Username)
            .ToListAsync(ct);

        var tenantCustomerIds = _db.Customers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id);

        var appointments = await (
            from appointment in _db.Appointments.AsNoTracking()
            join customer in _db.Customers.AsNoTracking()
                on appointment.CustomerId equals customer.Id
            join staff in _db.Staff.AsNoTracking()
                on appointment.StaffId equals staff.Id into staffGroup
            from staff in staffGroup.DefaultIfEmpty()
            where appointment.TenantId == tenantId
               && appointment.BranchId == branchId
               && appointment.CustomerId.HasValue
               && tenantCustomerIds.Contains(appointment.CustomerId ?? Guid.Empty)
               && appointment.StartAt >= scheduleDate
               && appointment.StartAt < nextDate
            select new AppointmentScheduleEntryDto
            {
                Id = appointment.Id,
                CustomerId = appointment.CustomerId,
                ServiceId = appointment.AppointmentItems
                    .Where(item => item.ItemType == "service")
                    .Select(item => (Guid?)item.ItemId)
                    .FirstOrDefault(),
                CustomerName = customer.FullName ?? "Unnamed customer",
                CustomerPhone = customer.Phone ?? string.Empty,
                ServiceName = appointment.AppointmentItems
                    .Where(item => item.ItemType == "service")
                    .Select(item => item.Name)
                    .FirstOrDefault() ?? string.Empty,
                ServicePrice = appointment.AppointmentItems
                    .Where(item => item.ItemType == "service")
                    .Select(item => (decimal?)item.UnitPrice)
                    .FirstOrDefault(),
                CurrencyCode = appointment.AppointmentItems
                    .Where(item => item.ItemType == "service")
                    .Select(item => item.CurrencyCode)
                    .FirstOrDefault() ?? "AED",
                ResourceName = staff != null ? staff.FullName : string.Empty,
                StartAt = appointment.StartAt,
                EndAt = appointment.EndAt,
                Status = appointment.Status,
                Notes = appointment.Notes ?? string.Empty
            }).ToListAsync(ct);

        var itemsByResource = appointments
            .GroupBy(item => string.IsNullOrWhiteSpace(item.ResourceName) ? ExtractResourceName(item.Notes) : item.ResourceName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new AppointmentScheduleEntryDto
                    {
                        Id = item.Id,
                        CustomerId = item.CustomerId,
                        ServiceId = item.ServiceId,
                        CustomerName = item.CustomerName,
                        CustomerPhone = item.CustomerPhone,
                        ServiceName = item.ServiceName,
                        ServicePrice = item.ServicePrice,
                        CurrencyCode = item.CurrencyCode,
                        ResourceName = string.IsNullOrWhiteSpace(item.ResourceName) ? ExtractResourceName(item.Notes) : item.ResourceName,
                        StartAt = item.StartAt,
                        EndAt = item.EndAt,
                        Status = item.Status,
                        Notes = StripResourcePrefix(item.Notes)
                    })
                    .OrderBy(item => item.StartAt)
                    .ThenBy(item => item.CustomerName)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var columns = resources
            .Select(resource => new AppointmentScheduleColumnDto
            {
                UserId = resource.UserId,
                ResourceName = resource.Username,
                Role = resource.Role,
                IsUnassigned = false,
                Items = itemsByResource.TryGetValue(resource.Username, out var resourceItems)
                    ? resourceItems
                    : []
            })
            .ToList();

        columns.Add(new AppointmentScheduleColumnDto
        {
            UserId = null,
            ResourceName = "Unassigned",
            Role = string.Empty,
            IsUnassigned = true,
            Items = itemsByResource.TryGetValue(string.Empty, out var unassignedItems)
                ? unassignedItems
                : []
        });

        return Ok(new AppointmentScheduleBoardDto
        {
            Date = scheduleDate,
            Columns = columns
        });
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant/branch context.");

        if (request.EndAt <= request.StartAt)
            return BadRequest("End time must be after start time.");

        if (request.CustomerId is null)
            return BadRequest("Customer is required before booking an appointment.");

        if (request.ServiceId is null)
            return BadRequest("Service is required before booking an appointment.");

        Guid? customerId = request.CustomerId;
        if (customerId.HasValue)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .Where(customerRecord =>
                    customerRecord.Id == customerId.Value &&
                    customerRecord.TenantId == _tenant.TenantId.Value)
                .Select(customerRecord => new
                {
                    customerRecord.Id,
                    customerRecord.FullName,
                    customerRecord.Phone,
                    customerRecord.IsActive
                })
                .SingleOrDefaultAsync(ct);

            if (customer is null)
                return BadRequest("Customer does not belong to the current tenant.");

            if (!customer.IsActive)
                return BadRequest("Customer must be active before booking an appointment.");

            if (string.IsNullOrWhiteSpace(customer.FullName) || string.IsNullOrWhiteSpace(customer.Phone))
                return BadRequest("Appointment customer must have a clear name and phone number.");
        }

        var normalizedResourceName = string.IsNullOrWhiteSpace(request.ResourceName)
            ? null
            : request.ResourceName.Trim();

        Staff? selectedStaff = null;

        if (!string.IsNullOrWhiteSpace(normalizedResourceName))
        {
            selectedStaff = await _db.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == _tenant.TenantId.Value &&
                    x.BranchId == _tenant.BranchId.Value &&
                    x.IsActive &&
                    x.IsBookableForAppointments &&
                    x.FullName == normalizedResourceName, ct);

            if (selectedStaff is null)
                return BadRequest("Selected resource is not assigned to this branch.");
        }

        var serviceSnapshot = await (
            from service in _db.Services.AsNoTracking()
            join price in _db.ServicePrices.AsNoTracking()
                on service.Id equals price.ServiceId
            where service.Id == request.ServiceId.Value
               && price.ServiceId == request.ServiceId.Value
               && price.TenantId == _tenant.TenantId.Value
               && price.BranchId == _tenant.BranchId.Value
               && service.TenantId == _tenant.TenantId.Value
               && service.BranchId == _tenant.BranchId.Value
               && service.IsActive
               && price.IsActive
            select new
            {
                service.Id,
                Name = service.NameEn ?? service.NameAr ?? "Unnamed service",
                price.PriceCents,
                price.CurrencyCode
            })
            .SingleOrDefaultAsync(ct);

        if (serviceSnapshot is null)
            return BadRequest("Selected service is not active for this branch.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? createdByUserId = Guid.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            BranchId = _tenant.BranchId.Value,
            CustomerId = customerId,
            StaffId = selectedStaff?.Id,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Status = "scheduled",
            Notes = BuildAppointmentNotes(selectedStaff?.FullName ?? normalizedResourceName, request.Notes),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        _db.AppointmentItems.Add(new AppointmentItem
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            ItemType = "service",
            ItemId = serviceSnapshot.Id,
            Name = serviceSnapshot.Name,
            Qty = 1,
            UnitPrice = serviceSnapshot.PriceCents / 100m,
            LineTotal = serviceSnapshot.PriceCents / 100m,
            CurrencyCode = string.IsNullOrWhiteSpace(serviceSnapshot.CurrencyCode) ? "AED" : serviceSnapshot.CurrencyCode
        });
        await _db.SaveChangesAsync(ct);

        return Ok(appointment.Id);
    }

    [HttpPost("{appointmentId:guid}/status")]
    public async Task<ActionResult<AppointmentListItemDto>> UpdateStatus(
        [FromRoute] Guid appointmentId,
        [FromBody] UpdateAppointmentStatusRequest request,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant/branch context.");

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
            return BadRequest("Status must be scheduled, confirmed, completed, cancelled, or no_show.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;
        var tenantCustomerIds = _db.Customers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id);

        var appointment = await _db.Appointments.FirstOrDefaultAsync(
            x => x.Id == appointmentId
                 && x.TenantId == tenantId
                 && x.BranchId == branchId
                 && x.CustomerId.HasValue
                 && tenantCustomerIds.Contains(x.CustomerId ?? Guid.Empty),
            ct);

        if (appointment is null)
            return NotFound("Appointment not found.");

        appointment.Status = normalizedStatus;
        await _db.SaveChangesAsync(ct);

        return Ok(await LoadAppointmentDtoAsync(appointmentId, tenantId, branchId, ct));
    }

    [HttpPost("{appointmentId:guid}")]
    public async Task<ActionResult<AppointmentListItemDto>> Update(
        [FromRoute] Guid appointmentId,
        [FromBody] UpdateAppointmentRequest request,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant/branch context.");

        if (request.EndAt <= request.StartAt)
            return BadRequest("End time must be after start time.");

        if (request.CustomerId is null)
            return BadRequest("Customer is required before booking an appointment.");

        if (request.ServiceId is null)
            return BadRequest("Service is required before booking an appointment.");

        var tenantId = _tenant.TenantId.Value;
        var branchId = _tenant.BranchId.Value;

        var appointment = await _db.Appointments
            .Include(x => x.AppointmentItems)
            .FirstOrDefaultAsync(x =>
                x.Id == appointmentId &&
                x.TenantId == tenantId &&
                x.BranchId == branchId,
                ct);

        if (appointment is null)
            return NotFound("Appointment not found.");

        var customer = await _db.Customers
            .AsNoTracking()
            .Where(customerRecord =>
                customerRecord.Id == request.CustomerId.Value &&
                customerRecord.TenantId == tenantId)
            .Select(customerRecord => new
            {
                customerRecord.Id,
                customerRecord.FullName,
                customerRecord.Phone,
                customerRecord.IsActive
            })
            .SingleOrDefaultAsync(ct);

        if (customer is null)
            return BadRequest("Customer does not belong to the current tenant.");

        if (!customer.IsActive)
            return BadRequest("Customer must be active before booking an appointment.");

        if (string.IsNullOrWhiteSpace(customer.FullName) || string.IsNullOrWhiteSpace(customer.Phone))
            return BadRequest("Appointment customer must have a clear name and phone number.");

        var normalizedResourceName = string.IsNullOrWhiteSpace(request.ResourceName)
            ? null
            : request.ResourceName.Trim();

        Staff? selectedStaff = null;

        if (!string.IsNullOrWhiteSpace(normalizedResourceName))
        {
            selectedStaff = await _db.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.BranchId == branchId &&
                    x.IsActive &&
                    x.IsBookableForAppointments &&
                    x.FullName == normalizedResourceName, ct);

            if (selectedStaff is null)
                return BadRequest("Selected resource is not assigned to this branch.");
        }

        var serviceSnapshot = await (
            from service in _db.Services.AsNoTracking()
            join price in _db.ServicePrices.AsNoTracking()
                on service.Id equals price.ServiceId
            where service.Id == request.ServiceId.Value
               && price.ServiceId == request.ServiceId.Value
               && price.TenantId == tenantId
               && price.BranchId == branchId
               && service.TenantId == tenantId
               && service.BranchId == branchId
               && service.IsActive
               && price.IsActive
            select new
            {
                service.Id,
                Name = service.NameEn ?? service.NameAr ?? "Unnamed service",
                price.PriceCents,
                price.CurrencyCode
            })
            .SingleOrDefaultAsync(ct);

        if (serviceSnapshot is null)
            return BadRequest("Selected service is not active for this branch.");

        appointment.CustomerId = request.CustomerId.Value;
        appointment.StaffId = selectedStaff?.Id;
        appointment.StartAt = request.StartAt;
        appointment.EndAt = request.EndAt;
        appointment.Notes = BuildAppointmentNotes(selectedStaff?.FullName ?? normalizedResourceName, request.Notes);

        var serviceItem = appointment.AppointmentItems.FirstOrDefault(item => item.ItemType == "service");
        if (serviceItem is null)
        {
            serviceItem = new AppointmentItem
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                ItemType = "service"
            };
            _db.AppointmentItems.Add(serviceItem);
        }

        serviceItem.ItemId = serviceSnapshot.Id;
        serviceItem.Name = serviceSnapshot.Name;
        serviceItem.Qty = 1;
        serviceItem.UnitPrice = serviceSnapshot.PriceCents / 100m;
        serviceItem.LineTotal = serviceSnapshot.PriceCents / 100m;
        serviceItem.CurrencyCode = string.IsNullOrWhiteSpace(serviceSnapshot.CurrencyCode) ? "AED" : serviceSnapshot.CurrencyCode;

        await _db.SaveChangesAsync(ct);
        return Ok(await LoadAppointmentDtoAsync(appointment.Id, tenantId, branchId, ct));
    }

    private static string? BuildAppointmentNotes(string? resourceName, string? notes)
    {
        var cleanNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (string.IsNullOrWhiteSpace(resourceName))
            return cleanNotes;

        return string.IsNullOrWhiteSpace(cleanNotes)
            ? $"[resource:{resourceName}]"
            : $"[resource:{resourceName}] {cleanNotes}";
    }

    private static string ExtractResourceName(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return string.Empty;

        var match = ResourcePrefixRegex.Match(notes);
        return match.Success ? match.Groups["resource"].Value.Trim() : string.Empty;
    }

    private static string StripResourcePrefix(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return string.Empty;

        return ResourcePrefixRegex.Replace(notes, string.Empty).Trim();
    }

    private async Task<AppointmentListItemDto> LoadAppointmentDtoAsync(Guid appointmentId, Guid tenantId, Guid branchId, CancellationToken ct)
    {
        var dto = await (
            from item in _db.Appointments.AsNoTracking()
            join customer in _db.Customers.AsNoTracking()
                on item.CustomerId equals customer.Id into customerGroup
            from customer in customerGroup.DefaultIfEmpty()
            join staff in _db.Staff.AsNoTracking()
                on item.StaffId equals staff.Id into staffGroup
            from staff in staffGroup.DefaultIfEmpty()
            where item.Id == appointmentId
               && item.TenantId == tenantId
               && item.BranchId == branchId
            select new AppointmentListItemDto
            {
                Id = item.Id,
                CustomerId = item.CustomerId,
                ServiceId = item.AppointmentItems
                    .Where(appointmentItem => appointmentItem.ItemType == "service")
                    .Select(appointmentItem => (Guid?)appointmentItem.ItemId)
                    .FirstOrDefault(),
                CustomerName = customer != null ? customer.FullName ?? "Unnamed customer" : string.Empty,
                CustomerPhone = customer != null ? customer.Phone ?? string.Empty : string.Empty,
                ServiceName = item.AppointmentItems
                    .Where(appointmentItem => appointmentItem.ItemType == "service")
                    .Select(appointmentItem => appointmentItem.Name)
                    .FirstOrDefault() ?? string.Empty,
                ServicePrice = item.AppointmentItems
                    .Where(appointmentItem => appointmentItem.ItemType == "service")
                    .Select(appointmentItem => (decimal?)appointmentItem.UnitPrice)
                    .FirstOrDefault(),
                CurrencyCode = item.AppointmentItems
                    .Where(appointmentItem => appointmentItem.ItemType == "service")
                    .Select(appointmentItem => appointmentItem.CurrencyCode)
                    .FirstOrDefault() ?? "AED",
                ResourceName = staff != null ? staff.FullName : string.Empty,
                StartAt = item.StartAt,
                EndAt = item.EndAt,
                Status = item.Status,
                Notes = item.Notes ?? string.Empty,
                ItemCount = item.AppointmentItems.Count,
                CreatedAt = item.CreatedAt
            }).FirstAsync(ct);

        dto.ResourceName = string.IsNullOrWhiteSpace(dto.ResourceName) ? ExtractResourceName(dto.Notes) : dto.ResourceName;
        dto.Notes = StripResourcePrefix(dto.Notes);
        return dto;
    }
}
