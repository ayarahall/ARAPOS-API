using Ayapos.Api.Contracts.Common;
using Ayapos.Api.Contracts.Expenses;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Ayapos.Api.Services.Expenses;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantslug}/expenses")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class ExpensesController : ControllerBase
{
    private static readonly string[] AllowedStatuses = ["draft", "submitted", "approved", "paid", "cancelled"];

    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IExpenseReceiptAnalyzer _expenseReceiptAnalyzer;

    public ExpensesController(AyaposDbContext db, ITenantContext tenant, IExpenseReceiptAnalyzer expenseReceiptAnalyzer)
    {
        _db = db;
        _tenant = tenant;
        _expenseReceiptAnalyzer = expenseReceiptAnalyzer;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BranchExpenseListItemDto>>> List(
        [FromQuery] string? category,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        var query = _db.BranchExpenses
            .AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim();
            query = query.Where(x => x.Category == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Title, $"%{term}%") ||
                EF.Functions.Like(x.Category, $"%{term}%") ||
                (x.Notes != null && EF.Functions.Like(x.Notes, $"%{term}%")) ||
                EF.Functions.Like(x.Status, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BranchExpenseListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Category = x.Category,
                Amount = x.Amount,
                CurrencyCode = x.CurrencyCode,
                ExpenseDate = x.ExpenseDate,
                Status = x.Status,
                Notes = x.Notes ?? string.Empty,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<BranchExpenseListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("ai-status")]
    public ActionResult<ExpenseAiStatusDto> GetAiStatus()
    {
        return Ok(new ExpenseAiStatusDto
        {
            Enabled = _expenseReceiptAnalyzer.IsConfigured,
            Message = _expenseReceiptAnalyzer.IsConfigured
                ? "AI receipt analysis is ready."
                : "AI receipt analysis is not configured yet. Add OpenAI:ApiKey or set OPENAI_API_KEY first."
        });
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBranchExpenseRequest request, CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var title = request.Title.Trim();
        var category = request.Category.Trim();
        var currencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "AED" : request.CurrencyCode.Trim().ToUpperInvariant();

        if (title.Length < 2)
            return BadRequest("Title is required.");

        if (category.Length < 2)
            return BadRequest("Category is required.");

        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? createdByUserId = Guid.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

        var expense = new BranchExpense
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            BranchId = _tenant.BranchId.Value,
            Title = title,
            Category = category,
            Amount = request.Amount,
            CurrencyCode = currencyCode,
            ExpenseDate = request.ExpenseDate == default ? DateTime.UtcNow : request.ExpenseDate,
            Status = "draft",
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.BranchExpenses.Add(expense);
        await _db.SaveChangesAsync(ct);

        return Ok(expense.Id);
    }

    [HttpPost("analyze-receipt")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<AnalyzeExpenseReceiptResponse>> AnalyzeReceipt([FromForm] IFormFile? file, CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        if (file is null || file.Length == 0)
            return BadRequest("Receipt file is required.");

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        var allowedContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Only JPG, PNG, or WEBP receipt images are supported right now.");

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);

        var result = await _expenseReceiptAnalyzer.AnalyzeAsync(file.FileName, contentType, memory.ToArray(), ct);
        return Ok(result);
    }

    [HttpPost("{expenseId:guid}/status")]
    public async Task<ActionResult<BranchExpenseListItemDto>> UpdateStatus(
        [FromRoute] Guid expenseId,
        [FromBody] UpdateBranchExpenseStatusRequest request,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
            return BadRequest("Status must be draft, submitted, approved, paid, or cancelled.");

        var expense = await _db.BranchExpenses.FirstOrDefaultAsync(
            x => x.Id == expenseId && x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value,
            ct);

        if (expense is null)
            return NotFound("Expense not found.");

        expense.Status = normalizedStatus;
        await _db.SaveChangesAsync(ct);

        return Ok(new BranchExpenseListItemDto
        {
            Id = expense.Id,
            Title = expense.Title,
            Category = expense.Category,
            Amount = expense.Amount,
            CurrencyCode = expense.CurrencyCode,
            ExpenseDate = expense.ExpenseDate,
            Status = expense.Status,
            Notes = expense.Notes ?? string.Empty,
            CreatedAt = expense.CreatedAt
        });
    }
}
