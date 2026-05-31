namespace Ayapos.EndUser.Models.Staff;

public sealed class CreateEmployeeDocumentRequestModel
{
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "general";
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
