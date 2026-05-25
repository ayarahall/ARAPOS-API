using Ayapos.Api.Contracts.Staff;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("tenant-admin/branches/{branchId:guid}/employees")]
[Authorize(Policy = AuthPolicies.TenantAdmin)]
public sealed class TenantEmployeesController : ControllerBase
{
    private static readonly string[] AllowedAttendanceStatuses = ["present", "late", "absent", "off", "leave"];

    private readonly AyaposDbContext _db;

    public TenantEmployeesController(AyaposDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeListItemDto>>> List([FromRoute] Guid branchId, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var today = DateTime.UtcNow.Date;

        var employees = await _db.Staff
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId)
            .OrderBy(x => x.FullName)
            .Select(x => new EmployeeListItemDto
            {
                Id = x.Id,
                BranchId = x.BranchId,
                LinkedUserId = x.LinkedUserId,
                FullName = x.FullName,
                Phone = x.Phone,
                Email = x.Email,
                EmployeeCode = x.EmployeeCode,
                JobTitle = x.JobTitle,
                EmploymentType = x.EmploymentType,
                SalaryType = x.SalaryType,
                BaseSalary = x.BaseSalary,
                DeductionPerLateMinute = x.DeductionPerLateMinute,
                DeductionPerAbsentDay = x.DeductionPerAbsentDay,
                WeeklyOffDays = x.WeeklyOffDays,
                HireDate = x.HireDate,
                PhotoUrl = x.PhotoUrl,
                Notes = x.Notes,
                IsBookableForAppointments = x.IsBookableForAppointments,
                TrackAttendance = x.TrackAttendance,
                IsActive = x.IsActive,
                AppointmentColor = x.AppointmentColor,
                HasSystemAccess = x.LinkedUserId != null,
                LinkedUsername = x.LinkedUser != null ? x.LinkedUser.Username : null,
                LinkedUserRole = x.LinkedUser != null ? x.LinkedUser.Role : null,
                TodayAttendanceStatus = x.Attendances.Where(a => a.AttendanceDate == today).Select(a => a.Status).FirstOrDefault(),
                TodayCheckInAt = x.Attendances.Where(a => a.AttendanceDate == today).Select(a => a.CheckInAt).FirstOrDefault(),
                TodayCheckOutAt = x.Attendances.Where(a => a.AttendanceDate == today).Select(a => a.CheckOutAt).FirstOrDefault(),
                TodayDeductionAmount = x.Attendances.Where(a => a.AttendanceDate == today).Select(a => (decimal?)a.DeductionAmount).FirstOrDefault() ?? 0m
            })
            .ToListAsync(ct);

        return Ok(employees);
    }

    [HttpGet("attendance-summary")]
    public async Task<ActionResult<BranchAttendanceSummaryDto>> GetAttendanceSummary([FromRoute] Guid branchId, [FromQuery] DateTime? date, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var attendanceDate = (date ?? DateTime.UtcNow).Date;
        var records = await _db.StaffAttendances
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId && x.AttendanceDate == attendanceDate)
            .ToListAsync(ct);

        var totalEmployees = await _db.Staff
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId.Value && x.BranchId == branchId && x.IsActive, ct);

        return Ok(new BranchAttendanceSummaryDto
        {
            Date = attendanceDate,
            TotalEmployees = totalEmployees,
            PresentCount = records.Count(x => x.Status == "present"),
            LateCount = records.Count(x => x.Status == "late"),
            AbsentCount = records.Count(x => x.Status == "absent"),
            LeaveCount = records.Count(x => x.Status == "leave"),
            TotalDeductions = records.Sum(x => x.DeductionAmount)
        });
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeListItemDto>> Create([FromRoute] Guid branchId, [FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == branchId && x.TenantId == tenantId.Value, ct);
        if (branch is null)
            return NotFound("Branch not found.");

        var normalizedName = request.FullName.Trim();
        if (normalizedName.Length < 2)
            return BadRequest("Employee full name is required.");

        if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
        {
            var duplicateCode = await _db.Staff.AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.BranchId == branchId &&
                x.EmployeeCode == request.EmployeeCode.Trim(), ct);

            if (duplicateCode)
                return Conflict("Employee code already exists in this branch.");
        }

        if (request.LinkedUserId.HasValue)
        {
            var userIsAssignedToBranch = await _db.BranchUserAssignments.AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.BranchId == branchId &&
                x.UserId == request.LinkedUserId.Value, ct);

            if (!userIsAssignedToBranch)
                return BadRequest("Linked user must already belong to this branch.");
        }

        var employee = new Staff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            BranchId = branchId,
            LinkedUserId = request.LinkedUserId,
            FullName = normalizedName,
            Phone = NormalizeOptional(request.Phone, 30),
            Email = NormalizeOptional(request.Email, 120),
            EmployeeCode = NormalizeOptional(request.EmployeeCode, 40),
            JobTitle = NormalizeOptional(request.JobTitle, 40),
            EmploymentType = NormalizeOptional(request.EmploymentType, 30) ?? "employee",
            SalaryType = NormalizeOptional(request.SalaryType, 20) ?? "monthly",
            BaseSalary = request.BaseSalary,
            DeductionPerLateMinute = request.DeductionPerLateMinute,
            DeductionPerAbsentDay = request.DeductionPerAbsentDay,
            WeeklyOffDays = NormalizeOptional(request.WeeklyOffDays, 40),
            HireDate = request.HireDate?.Date,
            PhotoUrl = NormalizeOptional(request.PhotoUrl, 260),
            Notes = NormalizeOptional(request.Notes, 500),
            IsBookableForAppointments = request.IsBookableForAppointments,
            TrackAttendance = request.TrackAttendance,
            IsActive = request.IsActive,
            AppointmentColor = NormalizeOptional(request.AppointmentColor, 20),
            CreatedAt = DateTime.UtcNow
        };

        _db.Staff.Add(employee);
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadEmployeeAsync(employee.Id, tenantId.Value, branchId, ct));
    }

    [HttpPost("{employeeId:guid}")]
    public async Task<ActionResult<EmployeeListItemDto>> Update([FromRoute] Guid branchId, [FromRoute] Guid employeeId, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var employee = await _db.Staff.FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId.Value && x.BranchId == branchId, ct);
        if (employee is null)
            return NotFound("Employee not found.");

        var normalizedName = request.FullName.Trim();
        if (normalizedName.Length < 2)
            return BadRequest("Employee full name is required.");

        if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
        {
            var duplicateCode = await _db.Staff.AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.BranchId == branchId &&
                x.EmployeeCode == request.EmployeeCode.Trim() &&
                x.Id != employeeId, ct);

            if (duplicateCode)
                return Conflict("Employee code already exists in this branch.");
        }

        if (request.LinkedUserId.HasValue)
        {
            var userIsAssignedToBranch = await _db.BranchUserAssignments.AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.BranchId == branchId &&
                x.UserId == request.LinkedUserId.Value, ct);

            if (!userIsAssignedToBranch)
                return BadRequest("Linked user must already belong to this branch.");
        }

        employee.LinkedUserId = request.LinkedUserId;
        employee.FullName = normalizedName;
        employee.Phone = NormalizeOptional(request.Phone, 30);
        employee.Email = NormalizeOptional(request.Email, 120);
        employee.EmployeeCode = NormalizeOptional(request.EmployeeCode, 40);
        employee.JobTitle = NormalizeOptional(request.JobTitle, 40);
        employee.EmploymentType = NormalizeOptional(request.EmploymentType, 30) ?? "employee";
        employee.SalaryType = NormalizeOptional(request.SalaryType, 20) ?? "monthly";
        employee.BaseSalary = request.BaseSalary;
        employee.DeductionPerLateMinute = request.DeductionPerLateMinute;
        employee.DeductionPerAbsentDay = request.DeductionPerAbsentDay;
        employee.WeeklyOffDays = NormalizeOptional(request.WeeklyOffDays, 40);
        employee.HireDate = request.HireDate?.Date;
        employee.PhotoUrl = NormalizeOptional(request.PhotoUrl, 260);
        employee.Notes = NormalizeOptional(request.Notes, 500);
        employee.IsBookableForAppointments = request.IsBookableForAppointments;
        employee.TrackAttendance = request.TrackAttendance;
        employee.IsActive = request.IsActive;
        employee.AppointmentColor = NormalizeOptional(request.AppointmentColor, 20);

        await _db.SaveChangesAsync(ct);
        return Ok(await LoadEmployeeAsync(employee.Id, tenantId.Value, branchId, ct));
    }

    [HttpGet("{employeeId:guid}/attendance")]
    public async Task<ActionResult<IReadOnlyList<EmployeeAttendanceDto>>> GetAttendance([FromRoute] Guid branchId, [FromRoute] Guid employeeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var startDate = (from ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        var endDate = (to ?? DateTime.UtcNow.Date.AddDays(1)).Date;

        var items = await _db.StaffAttendances
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId && x.StaffId == employeeId && x.AttendanceDate >= startDate && x.AttendanceDate < endDate)
            .OrderByDescending(x => x.AttendanceDate)
            .Select(x => new EmployeeAttendanceDto
            {
                Id = x.Id,
                StaffId = x.StaffId,
                ShiftId = x.ShiftId,
                AttendanceDate = x.AttendanceDate,
                CheckInAt = x.CheckInAt,
                CheckOutAt = x.CheckOutAt,
                Status = x.Status,
                LateMinutes = x.LateMinutes,
                WorkedMinutes = x.WorkedMinutes,
                DeductionAmount = x.DeductionAmount,
                Notes = x.Notes,
                ShiftName = x.Shift != null ? x.Shift.Name : null
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("{employeeId:guid}/attendance/check-in")]
    public async Task<ActionResult<EmployeeAttendanceDto>> CheckIn([FromRoute] Guid branchId, [FromRoute] Guid employeeId, [FromBody] EmployeeAttendanceCheckInRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var employee = await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId.Value && x.BranchId == branchId, ct);
        if (employee is null)
            return NotFound("Employee not found.");

        var attendanceDate = (request.AttendanceDate ?? DateTime.UtcNow).Date;
        var checkInAt = request.CheckInAt ?? DateTime.UtcNow;
        var shift = await LoadCurrentShiftAsync(tenantId.Value, branchId, employeeId, attendanceDate, ct);
        var lateMinutes = CalculateLateMinutes(shift, checkInAt);

        var attendance = await _db.StaffAttendances.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId.Value &&
            x.BranchId == branchId &&
            x.StaffId == employeeId &&
            x.AttendanceDate == attendanceDate, ct);

        if (attendance is null)
        {
            attendance = new StaffAttendance
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                BranchId = branchId,
                StaffId = employeeId,
                AttendanceDate = attendanceDate,
                CreatedAt = DateTime.UtcNow
            };
            _db.StaffAttendances.Add(attendance);
        }

        attendance.ShiftId = shift?.Id;
        attendance.CheckInAt = checkInAt;
        attendance.Status = lateMinutes > 0 ? "late" : "present";
        attendance.LateMinutes = lateMinutes;
        attendance.DeductionAmount = lateMinutes > 0 ? Math.Round((employee.DeductionPerLateMinute ?? 0m) * lateMinutes, 2) : 0m;
        attendance.Notes = MergeNotes(attendance.Notes, request.Notes);

        await _db.SaveChangesAsync(ct);
        return Ok(await LoadAttendanceAsync(attendance.Id, ct));
    }

    [HttpPost("{employeeId:guid}/attendance/check-out")]
    public async Task<ActionResult<EmployeeAttendanceDto>> CheckOut([FromRoute] Guid branchId, [FromRoute] Guid employeeId, [FromBody] EmployeeAttendanceCheckOutRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var attendanceDate = (request.AttendanceDate ?? DateTime.UtcNow).Date;
        var attendance = await _db.StaffAttendances.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId.Value &&
            x.BranchId == branchId &&
            x.StaffId == employeeId &&
            x.AttendanceDate == attendanceDate, ct);

        if (attendance is null)
            return BadRequest("Check-in must be recorded before check-out.");

        var checkOutAt = request.CheckOutAt ?? DateTime.UtcNow;
        attendance.CheckOutAt = checkOutAt;
        if (attendance.CheckInAt.HasValue && checkOutAt > attendance.CheckInAt.Value)
            attendance.WorkedMinutes = (int)Math.Round((checkOutAt - attendance.CheckInAt.Value).TotalMinutes);
        attendance.Notes = MergeNotes(attendance.Notes, request.Notes);

        await _db.SaveChangesAsync(ct);
        return Ok(await LoadAttendanceAsync(attendance.Id, ct));
    }

    [HttpPost("{employeeId:guid}/attendance/mark")]
    public async Task<ActionResult<EmployeeAttendanceDto>> MarkAttendance([FromRoute] Guid branchId, [FromRoute] Guid employeeId, [FromBody] MarkEmployeeAttendanceRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        if (!AllowedAttendanceStatuses.Contains(normalizedStatus))
            return BadRequest("Attendance status must be present, late, absent, off, or leave.");

        var employee = await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId.Value && x.BranchId == branchId, ct);
        if (employee is null)
            return NotFound("Employee not found.");

        var attendanceDate = request.AttendanceDate.Date;
        var attendance = await _db.StaffAttendances.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId.Value &&
            x.BranchId == branchId &&
            x.StaffId == employeeId &&
            x.AttendanceDate == attendanceDate, ct);

        if (attendance is null)
        {
            attendance = new StaffAttendance
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                BranchId = branchId,
                StaffId = employeeId,
                AttendanceDate = attendanceDate,
                CreatedAt = DateTime.UtcNow
            };
            _db.StaffAttendances.Add(attendance);
        }

        attendance.Status = normalizedStatus;
        attendance.Notes = request.Notes?.Trim();
        attendance.DeductionAmount = normalizedStatus == "absent"
            ? Math.Round(employee.DeductionPerAbsentDay ?? 0m, 2)
            : 0m;

        await _db.SaveChangesAsync(ct);
        return Ok(await LoadAttendanceAsync(attendance.Id, ct));
    }

    [HttpGet("{employeeId:guid}/shifts")]
    public async Task<ActionResult<IReadOnlyList<EmployeeShiftDto>>> GetShifts([FromRoute] Guid branchId, [FromRoute] Guid employeeId, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var items = await _db.StaffShifts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId && x.StaffId == employeeId)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.StartTime)
            .Select(x => new EmployeeShiftDto
            {
                Id = x.Id,
                StaffId = x.StaffId,
                Name = x.Name,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                GraceMinutes = x.GraceMinutes,
                IsActive = x.IsActive,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                WeeklyPattern = x.WeeklyPattern
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("{employeeId:guid}/shifts")]
    public async Task<ActionResult<EmployeeShiftDto>> CreateShift([FromRoute] Guid branchId, [FromRoute] Guid employeeId, [FromBody] CreateEmployeeShiftRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var employeeExists = await _db.Staff.AsNoTracking().AnyAsync(x => x.Id == employeeId && x.TenantId == tenantId.Value && x.BranchId == branchId, ct);
        if (!employeeExists)
            return NotFound("Employee not found.");

        var shift = new StaffShift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            BranchId = branchId,
            StaffId = employeeId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Shift" : request.Name.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            GraceMinutes = Math.Max(0, request.GraceMinutes),
            IsActive = request.IsActive,
            EffectiveFrom = request.EffectiveFrom?.Date,
            EffectiveTo = request.EffectiveTo?.Date,
            WeeklyPattern = NormalizeOptional(request.WeeklyPattern, 40),
            CreatedAt = DateTime.UtcNow
        };

        _db.StaffShifts.Add(shift);
        await _db.SaveChangesAsync(ct);

        return Ok(new EmployeeShiftDto
        {
            Id = shift.Id,
            StaffId = shift.StaffId,
            Name = shift.Name,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            GraceMinutes = shift.GraceMinutes,
            IsActive = shift.IsActive,
            EffectiveFrom = shift.EffectiveFrom,
            EffectiveTo = shift.EffectiveTo,
            WeeklyPattern = shift.WeeklyPattern
        });
    }

    [HttpGet("{employeeId:guid}/leaves")]
    public async Task<ActionResult<IReadOnlyList<EmployeeLeaveDto>>> GetLeaves([FromRoute] Guid branchId, [FromRoute] Guid employeeId, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var items = await _db.StaffLeaves
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId && x.StaffId == employeeId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new EmployeeLeaveDto
            {
                Id = x.Id,
                StaffId = x.StaffId,
                LeaveType = x.LeaveType,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsPaid = x.IsPaid,
                Status = x.Status,
                Notes = x.Notes
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("{employeeId:guid}/leaves")]
    public async Task<ActionResult<EmployeeLeaveDto>> CreateLeave([FromRoute] Guid branchId, [FromRoute] Guid employeeId, [FromBody] CreateEmployeeLeaveRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        if (request.EndDate.Date < request.StartDate.Date)
            return BadRequest("Leave end date must be on or after start date.");

        var employeeExists = await _db.Staff.AsNoTracking().AnyAsync(x => x.Id == employeeId && x.TenantId == tenantId.Value && x.BranchId == branchId, ct);
        if (!employeeExists)
            return NotFound("Employee not found.");

        var leave = new StaffLeave
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            BranchId = branchId,
            StaffId = employeeId,
            LeaveType = string.IsNullOrWhiteSpace(request.LeaveType) ? "leave" : request.LeaveType.Trim().ToLowerInvariant(),
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            IsPaid = request.IsPaid,
            Status = "approved",
            Notes = NormalizeOptional(request.Notes, 400),
            CreatedAt = DateTime.UtcNow
        };

        _db.StaffLeaves.Add(leave);
        await _db.SaveChangesAsync(ct);

        return Ok(new EmployeeLeaveDto
        {
            Id = leave.Id,
            StaffId = leave.StaffId,
            LeaveType = leave.LeaveType,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            IsPaid = leave.IsPaid,
            Status = leave.Status,
            Notes = leave.Notes
        });
    }

    [HttpGet("{employeeId:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<EmployeeDocumentDto>>> GetDocuments([FromRoute] Guid branchId, [FromRoute] Guid employeeId, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var items = await _db.StaffDocuments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId.Value && x.BranchId == branchId && x.StaffId == employeeId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new EmployeeDocumentDto
            {
                Id = x.Id,
                StaffId = x.StaffId,
                Title = x.Title,
                DocumentType = x.DocumentType,
                FileName = x.FileName,
                FileUrl = x.FileUrl,
                ExpiresAt = x.ExpiresAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("{employeeId:guid}/documents")]
    public async Task<ActionResult<EmployeeDocumentDto>> CreateDocument([FromRoute] Guid branchId, [FromRoute] Guid employeeId, [FromBody] CreateEmployeeDocumentRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Unauthorized("Invalid tenant context.");

        var employeeExists = await _db.Staff.AsNoTracking().AnyAsync(x => x.Id == employeeId && x.TenantId == tenantId.Value && x.BranchId == branchId, ct);
        if (!employeeExists)
            return NotFound("Employee not found.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Document title is required.");

        var document = new StaffDocument
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            BranchId = branchId,
            StaffId = employeeId,
            Title = request.Title.Trim(),
            DocumentType = string.IsNullOrWhiteSpace(request.DocumentType) ? "general" : request.DocumentType.Trim().ToLowerInvariant(),
            FileName = NormalizeOptional(request.FileName, 160),
            FileUrl = NormalizeOptional(request.FileUrl, 260),
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.StaffDocuments.Add(document);
        await _db.SaveChangesAsync(ct);

        return Ok(new EmployeeDocumentDto
        {
            Id = document.Id,
            StaffId = document.StaffId,
            Title = document.Title,
            DocumentType = document.DocumentType,
            FileName = document.FileName,
            FileUrl = document.FileUrl,
            ExpiresAt = document.ExpiresAt
        });
    }

    private Guid? GetTenantId()
    {
        var claimValue = User.FindFirst("tenantId")?.Value;
        return Guid.TryParse(claimValue, out var tenantId) ? tenantId : null;
    }

    private async Task<EmployeeListItemDto> LoadEmployeeAsync(Guid employeeId, Guid tenantId, Guid branchId, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        return await _db.Staff
            .AsNoTracking()
            .Where(x => x.Id == employeeId && x.TenantId == tenantId && x.BranchId == branchId)
            .Select(x => new EmployeeListItemDto
            {
                Id = x.Id,
                BranchId = x.BranchId,
                LinkedUserId = x.LinkedUserId,
                FullName = x.FullName,
                Phone = x.Phone,
                Email = x.Email,
                EmployeeCode = x.EmployeeCode,
                JobTitle = x.JobTitle,
                EmploymentType = x.EmploymentType,
                SalaryType = x.SalaryType,
                BaseSalary = x.BaseSalary,
                DeductionPerLateMinute = x.DeductionPerLateMinute,
                DeductionPerAbsentDay = x.DeductionPerAbsentDay,
                WeeklyOffDays = x.WeeklyOffDays,
                HireDate = x.HireDate,
                PhotoUrl = x.PhotoUrl,
                Notes = x.Notes,
                IsBookableForAppointments = x.IsBookableForAppointments,
                TrackAttendance = x.TrackAttendance,
                IsActive = x.IsActive,
                AppointmentColor = x.AppointmentColor,
                HasSystemAccess = x.LinkedUserId != null,
                LinkedUsername = x.LinkedUser != null ? x.LinkedUser.Username : null,
                LinkedUserRole = x.LinkedUser != null ? x.LinkedUser.Role : null,
                TodayAttendanceStatus = x.Attendances.Where(a => a.AttendanceDate == today).Select(a => a.Status).FirstOrDefault(),
                TodayCheckInAt = x.Attendances.Where(a => a.AttendanceDate == today).Select(a => a.CheckInAt).FirstOrDefault(),
                TodayCheckOutAt = x.Attendances.Where(a => a.AttendanceDate == today).Select(a => a.CheckOutAt).FirstOrDefault(),
                TodayDeductionAmount = x.Attendances.Where(a => a.AttendanceDate == today).Select(a => (decimal?)a.DeductionAmount).FirstOrDefault() ?? 0m
            })
            .FirstAsync(ct);
    }

    private async Task<EmployeeAttendanceDto> LoadAttendanceAsync(Guid attendanceId, CancellationToken ct)
    {
        return await _db.StaffAttendances
            .AsNoTracking()
            .Where(x => x.Id == attendanceId)
            .Select(x => new EmployeeAttendanceDto
            {
                Id = x.Id,
                StaffId = x.StaffId,
                ShiftId = x.ShiftId,
                AttendanceDate = x.AttendanceDate,
                CheckInAt = x.CheckInAt,
                CheckOutAt = x.CheckOutAt,
                Status = x.Status,
                LateMinutes = x.LateMinutes,
                WorkedMinutes = x.WorkedMinutes,
                DeductionAmount = x.DeductionAmount,
                Notes = x.Notes,
                ShiftName = x.Shift != null ? x.Shift.Name : null
            })
            .FirstAsync(ct);
    }

    private async Task<StaffShift?> LoadCurrentShiftAsync(Guid tenantId, Guid branchId, Guid employeeId, DateTime attendanceDate, CancellationToken ct)
    {
        return await _db.StaffShifts
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.BranchId == branchId &&
                x.StaffId == employeeId &&
                x.IsActive &&
                (!x.EffectiveFrom.HasValue || x.EffectiveFrom.Value.Date <= attendanceDate) &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= attendanceDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    private static int CalculateLateMinutes(StaffShift? shift, DateTime checkInAt)
    {
        if (shift is null)
            return 0;

        var expectedCheckIn = checkInAt.Date + shift.StartTime + TimeSpan.FromMinutes(Math.Max(0, shift.GraceMinutes));
        if (checkInAt <= expectedCheckIn)
            return 0;

        return (int)Math.Round((checkInAt - expectedCheckIn).TotalMinutes);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? MergeNotes(string? current, string? incoming)
    {
        var normalizedIncoming = string.IsNullOrWhiteSpace(incoming) ? null : incoming.Trim();
        if (string.IsNullOrWhiteSpace(normalizedIncoming))
            return current;

        if (string.IsNullOrWhiteSpace(current))
            return normalizedIncoming;

        return $"{current} | {normalizedIncoming}";
    }
}
