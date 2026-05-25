namespace Ayapos.Api.Contracts.Staff;

public sealed class CreateEmployeeDocumentRequest
{
    public string Title { get; init; } = "";
    public string DocumentType { get; init; } = "";
    public string? FileName { get; init; }
    public string? FileUrl { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
