namespace Ayapos.EndUser.Models.Staff;

public sealed class EmployeeDocumentModel
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
