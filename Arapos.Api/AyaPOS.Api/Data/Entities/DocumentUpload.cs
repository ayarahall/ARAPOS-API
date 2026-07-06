namespace Ayapos.Api.Data.Entities;

public partial class DocumentUpload
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid? UploadedByUserId { get; set; }

    // SCANNED_FORM | CERTIFICATE | CONSENT_FORM | REPORT | OTHER
    public string DocumentType { get; set; } = "OTHER";

    public string OriginalFileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    // Original file bytes. Phase 1 storage backend: the row itself (Postgres bytea).
    // Kept behind this single column so a later phase can swap to disk/object storage
    // without changing the API surface — only this entity + the controller's read/write would move.
    public byte[] FileContent { get; set; } = null!;

    // ar | en | auto
    public string LanguageHint { get; set; } = "auto";

    // PENDING | PROCESSING | EXTRACTED | REVIEWED | APPROVED | FAILED
    public string Status { get; set; } = "PENDING";

    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
