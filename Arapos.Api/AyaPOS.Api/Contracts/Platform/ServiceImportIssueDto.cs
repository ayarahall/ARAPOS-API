namespace Ayapos.Api.Contracts.Platform;

public sealed class ServiceImportIssueDto
{
    public int RowNumber { get; init; }
    public string Message { get; init; } = string.Empty;
}
