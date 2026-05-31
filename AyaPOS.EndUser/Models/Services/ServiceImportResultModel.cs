namespace Ayapos.EndUser.Models.Services;

public sealed class ServiceImportResultModel
{
    public int TotalRows { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<ServiceImportIssueModel> Issues { get; set; } = [];
}
