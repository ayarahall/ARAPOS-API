namespace Ayapos.Api.Data.Entities;

public partial class DocumentAuditLog
{
    public Guid Id { get; set; }

    public Guid DocumentUploadId { get; set; }

    // UPLOADED | VIEWED_ORIGINAL | STATUS_CHANGED | OCR_STARTED | OCR_COMPLETED | OCR_FAILED |
    // AI_EXTRACTION_COMPLETED | FIELD_EDITED | REVIEWED | APPROVED | REJECTED | SAVED_TO_SYSTEM
    public string Action { get; set; } = null!;

    public Guid? ActorUserId { get; set; }

    public string? DetailsJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
