namespace Ayapos.Api.Contracts.Services;

public sealed class CreateServiceRequest
{
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public int? DurationMin { get; init; }
    public decimal Price { get; init; }
    public string? CurrencyCode { get; init; }
    public bool IsActive { get; init; } = true;
}
