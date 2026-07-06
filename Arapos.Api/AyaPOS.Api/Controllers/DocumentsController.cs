using System.Security.Claims;
using System.Text.Json;
using Ayapos.Api.Contracts.Common;
using Ayapos.Api.Contracts.Documents;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Ayapos.Api.Tenancy;
using Ayapos.Api.Tenancy.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

// Phase 1 of the document-upload / AI-extraction feature: upload, store, list, view, audit.
// No OCR / AI structuring yet — that lands in later phases once this foundation is solid.
[ApiController]
[Route("t/{tenantslug}/documents")]
[Authorize(Policy = AuthPolicies.TenantCashier)]
[RequireBranchHeader]
public sealed class DocumentsController : ControllerBase
{
    private const long MaxFileSizeBytes = 15_000_000; // 15 MB

    private static readonly string[] AllowedDocumentTypes =
        ["SCANNED_FORM", "CERTIFICATE", "CONSENT_FORM", "REPORT", "SERVICE_RECEIPT", "OTHER"];

    private static readonly string[] AllowedLanguageHints = ["ar", "en", "auto"];

    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png"
    ];

    private readonly AyaposDbContext _db;
    private readonly ITenantContext _tenant;

    public DocumentsController(AyaposDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private Guid? CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentUploadListItemDto>>> List(
        [FromQuery] string? documentType,
        [FromQuery] string? status,
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

        var query = _db.DocumentUploads
            .AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value);

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            var normalized = documentType.Trim().ToUpperInvariant();
            query = query.Where(x => x.DocumentType == normalized);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToUpperInvariant();
            query = query.Where(x => x.Status == normalized);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => EF.Functions.Like(x.OriginalFileName, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DocumentUploadListItemDto
            {
                Id = x.Id,
                DocumentType = x.DocumentType,
                OriginalFileName = x.OriginalFileName,
                MimeType = x.MimeType,
                FileSizeBytes = x.FileSizeBytes,
                LanguageHint = x.LanguageHint,
                Status = x.Status,
                FailureReason = x.FailureReason,
                ExtractedText = x.ExtractedText,
                ExtractedFieldsJson = x.ExtractedFieldsJson,
                ReviewedFieldsJson = x.ReviewedFieldsJson,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<DocumentUploadListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentUploadListItemDto>> Get([FromRoute] Guid id, CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var doc = await _db.DocumentUploads
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value)
            .Select(x => new DocumentUploadListItemDto
            {
                Id = x.Id,
                DocumentType = x.DocumentType,
                OriginalFileName = x.OriginalFileName,
                MimeType = x.MimeType,
                FileSizeBytes = x.FileSizeBytes,
                LanguageHint = x.LanguageHint,
                Status = x.Status,
                FailureReason = x.FailureReason,
                ExtractedText = x.ExtractedText,
                ExtractedFieldsJson = x.ExtractedFieldsJson,
                ReviewedFieldsJson = x.ReviewedFieldsJson,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (doc is null)
            return NotFound("Document not found.");

        return Ok(doc);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<DocumentUploadListItemDto>> Upload(
        [FromForm] UploadDocumentForm request,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var file = request.File;
        if (file is null || file.Length == 0)
            return BadRequest("A file is required.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File exceeds the 15 MB size limit.");

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        if (!AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Only PDF, JPG, or PNG files are supported.");

        var documentType = string.IsNullOrWhiteSpace(request.DocumentType)
            ? "OTHER"
            : request.DocumentType.Trim().ToUpperInvariant();
        if (!AllowedDocumentTypes.Contains(documentType))
            return BadRequest("documentType must be one of: " + string.Join(", ", AllowedDocumentTypes));

        var languageHint = string.IsNullOrWhiteSpace(request.LanguageHint)
            ? "auto"
            : request.LanguageHint.Trim().ToLowerInvariant();
        if (!AllowedLanguageHints.Contains(languageHint))
            return BadRequest("languageHint must be one of: ar, en, auto");

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);
        var bytes = memory.ToArray();

        var uploadedByUserId = CurrentUserId;
        var now = DateTime.UtcNow;

        var doc = new DocumentUpload
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId.Value,
            BranchId = _tenant.BranchId.Value,
            UploadedByUserId = uploadedByUserId,
            DocumentType = documentType,
            OriginalFileName = string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName.Trim(),
            MimeType = contentType,
            FileSizeBytes = bytes.LongLength,
            FileContent = bytes,
            LanguageHint = languageHint,
            Status = "PENDING",
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.DocumentUploads.Add(doc);

        _db.DocumentAuditLogs.Add(new DocumentAuditLog
        {
            Id = Guid.NewGuid(),
            DocumentUploadId = doc.Id,
            Action = "UPLOADED",
            ActorUserId = uploadedByUserId,
            DetailsJson = JsonSerializer.Serialize(new
            {
                fileName = doc.OriginalFileName,
                sizeBytes = doc.FileSizeBytes,
                documentType = doc.DocumentType
            }),
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(ct);

        return Ok(ToListItemDto(doc));
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> GetFile([FromRoute] Guid id, CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var doc = await _db.DocumentUploads.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value, ct);

        if (doc is null)
            return NotFound("Document not found.");

        _db.DocumentAuditLogs.Add(new DocumentAuditLog
        {
            Id = Guid.NewGuid(),
            DocumentUploadId = doc.Id,
            Action = "VIEWED_ORIGINAL",
            ActorUserId = CurrentUserId,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        return File(doc.FileContent, doc.MimeType, doc.OriginalFileName);
    }

    [HttpGet("{id:guid}/audit-log")]
    public async Task<ActionResult<List<DocumentAuditLogDto>>> GetAuditLog([FromRoute] Guid id, CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var exists = await _db.DocumentUploads.AsNoTracking().AnyAsync(
            x => x.Id == id && x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value, ct);
        if (!exists)
            return NotFound("Document not found.");

        var logs = await _db.DocumentAuditLogs
            .AsNoTracking()
            .Where(x => x.DocumentUploadId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DocumentAuditLogDto
            {
                Id = x.Id,
                Action = x.Action,
                ActorUserId = x.ActorUserId,
                DetailsJson = x.DetailsJson,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(logs);
    }

    [HttpPut("{id:guid}/review")]
    public async Task<ActionResult<DocumentUploadListItemDto>> Review(
        [FromRoute] Guid id,
        [FromBody] ReviewDocumentRequest request,
        CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var doc = await _db.DocumentUploads.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value, ct);

        if (doc is null)
            return NotFound("Document not found.");

        var reviewedJson = JsonSerializer.Serialize(request.Fields);
        doc.ReviewedFieldsJson = reviewedJson;
        doc.Status = "REVIEWED";
        doc.UpdatedAt = DateTime.UtcNow;

        _db.DocumentAuditLogs.Add(new DocumentAuditLog
        {
            Id = Guid.NewGuid(),
            DocumentUploadId = doc.Id,
            Action = "REVIEWED",
            ActorUserId = CurrentUserId,
            DetailsJson = reviewedJson,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);

        return Ok(ToListItemDto(doc));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<DocumentUploadListItemDto>> Approve([FromRoute] Guid id, CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var doc = await _db.DocumentUploads.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value, ct);

        if (doc is null)
            return NotFound("Document not found.");

        // Never approve straight from OCR/AI output — a human review pass must have happened first.
        if (string.IsNullOrWhiteSpace(doc.ReviewedFieldsJson))
            return BadRequest("Review the extracted fields before approving.");

        doc.Status = "APPROVED";
        doc.UpdatedAt = DateTime.UtcNow;

        _db.DocumentAuditLogs.Add(new DocumentAuditLog
        {
            Id = Guid.NewGuid(),
            DocumentUploadId = doc.Id,
            Action = "APPROVED",
            ActorUserId = CurrentUserId,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);

        return Ok(ToListItemDto(doc));
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult<DocumentUploadListItemDto>> Retry([FromRoute] Guid id, CancellationToken ct = default)
    {
        if (_tenant.TenantId is null || _tenant.BranchId is null)
            return Unauthorized("Missing tenant or branch.");

        var doc = await _db.DocumentUploads.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == _tenant.TenantId.Value && x.BranchId == _tenant.BranchId.Value, ct);

        if (doc is null)
            return NotFound("Document not found.");

        if (doc.Status != "FAILED")
            return BadRequest("Only failed documents can be retried.");

        doc.Status = "PENDING";
        doc.FailureReason = null;
        doc.UpdatedAt = DateTime.UtcNow;

        _db.DocumentAuditLogs.Add(new DocumentAuditLog
        {
            Id = Guid.NewGuid(),
            DocumentUploadId = doc.Id,
            Action = "UPLOADED",
            ActorUserId = CurrentUserId,
            DetailsJson = "{\"retry\":true}",
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);

        return Ok(ToListItemDto(doc));
    }

    private static DocumentUploadListItemDto ToListItemDto(DocumentUpload x) => new()
    {
        Id = x.Id,
        DocumentType = x.DocumentType,
        OriginalFileName = x.OriginalFileName,
        MimeType = x.MimeType,
        FileSizeBytes = x.FileSizeBytes,
        LanguageHint = x.LanguageHint,
        Status = x.Status,
        FailureReason = x.FailureReason,
        ExtractedText = x.ExtractedText,
        ExtractedFieldsJson = x.ExtractedFieldsJson,
        ReviewedFieldsJson = x.ReviewedFieldsJson,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}

public sealed class UploadDocumentForm
{
    public IFormFile? File { get; init; }
    public string? DocumentType { get; init; }
    public string? LanguageHint { get; init; }
}
