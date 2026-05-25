namespace Ayapos.Api.Contracts.Platform;

public sealed class ServiceImportRowDto
{
    public int RowNumber { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public decimal? Price { get; init; }
    public int? DurationMin { get; init; }
    public string CurrencyCode { get; init; } = "AED";
}
