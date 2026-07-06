using System.Text.Json;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Services.Documents;

/// <summary>
/// Polls for PENDING document uploads and runs OCR + rule-based field extraction on them.
/// No external queue (RabbitMQ/etc.) for v1 — a single Render instance polling every few
/// seconds is plenty at this volume, and it keeps deployment to one Docker service.
/// </summary>
public sealed class DocumentProcessingWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<DocumentProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Document processing batch failed unexpectedly.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }
    }

    private async Task ProcessPendingBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AyaposDbContext>();
        var ocr = scope.ServiceProvider.GetRequiredService<IOcrService>();
        var extractor = scope.ServiceProvider.GetRequiredService<IStructuredFieldExtractor>();

        // The worker runs outside any HTTP request, so there is no "current tenant" —
        // IgnoreQueryFilters() is required here (same pattern used by AuthController /
        // TenantAdminController for cross-tenant queries), otherwise EF Core's global
        // tenant filter throws trying to read a null ITenantContext.TenantId.
        var pending = await db.DocumentUploads
            .IgnoreQueryFilters()
            .Where(x => x.Status == "PENDING")
            .OrderBy(x => x.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var doc in pending)
        {
            await ProcessOneAsync(db, ocr, extractor, doc, ct);
        }
    }

    private async Task ProcessOneAsync(
        AyaposDbContext db,
        IOcrService ocr,
        IStructuredFieldExtractor extractor,
        DocumentUpload doc,
        CancellationToken ct)
    {
        doc.Status = "PROCESSING";
        doc.UpdatedAt = DateTime.UtcNow;
        db.DocumentAuditLogs.Add(NewLog(doc.Id, "OCR_STARTED", null));
        await db.SaveChangesAsync(ct);

        try
        {
            var text = await ocr.ExtractTextAsync(doc.FileContent, doc.MimeType, doc.LanguageHint, ct);
            doc.ExtractedText = text;

            var fieldsJson = extractor.ExtractFieldsJson(doc.DocumentType, text);
            doc.ExtractedFieldsJson = fieldsJson;

            doc.Status = "EXTRACTED";
            doc.UpdatedAt = DateTime.UtcNow;

            db.DocumentAuditLogs.Add(NewLog(doc.Id, "OCR_COMPLETED", new { textLength = text.Length }));
            db.DocumentAuditLogs.Add(NewLog(doc.Id, "AI_EXTRACTION_COMPLETED", new { fields = fieldsJson }));
        }
        catch (Exception ex)
        {
            doc.Status = "FAILED";
            doc.FailureReason = Truncate(ex.Message, 400);
            doc.UpdatedAt = DateTime.UtcNow;
            db.DocumentAuditLogs.Add(NewLog(doc.Id, "OCR_FAILED", new { error = ex.Message }));
            _logger.LogWarning(ex, "OCR failed for document {DocumentId}", doc.Id);
        }

        await db.SaveChangesAsync(ct);
    }

    private static DocumentAuditLog NewLog(Guid documentId, string action, object? details) => new()
    {
        Id = Guid.NewGuid(),
        DocumentUploadId = documentId,
        Action = action,
        ActorUserId = null,
        DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
        CreatedAt = DateTime.UtcNow,
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
