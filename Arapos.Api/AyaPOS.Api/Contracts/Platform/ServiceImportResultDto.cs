namespace Ayapos.Api.Contracts.Platform;

public sealed class ServiceImportResultDto
{
    public int TotalRows { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<ServiceImportIssueDto> Issues { get; set; } = [];
}
