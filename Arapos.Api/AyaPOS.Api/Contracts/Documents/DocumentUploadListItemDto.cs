namespace Ayapos.Api.Contracts.Documents;

public sealed class DocumentUploadListItemDto
{
    public Guid Id { get; init; }
    public string DocumentType { get; init; } = "OTHER";
    public string OriginalFileName { get; init; } = "";
    public string MimeType { get; init; } = "";
    public long FileSizeBytes { get; init; }
    public string LanguageHint { get; init; } = "auto";
    public string Status { get; init; } = "PENDING";
    public string? FailureReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
