namespace Ayapos.Api.Contracts.Staff;

public sealed class EmployeeDocumentDto
{
    public Guid Id { get; init; }
    public Guid StaffId { get; init; }
    public string Title { get; init; } = "";
    public string DocumentType { get; init; } = "";
    public string? FileName { get; init; }
    public string? FileUrl { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
