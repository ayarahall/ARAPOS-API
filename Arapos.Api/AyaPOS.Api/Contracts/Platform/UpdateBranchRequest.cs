namespace Ayapos.Api.Contracts.Platform;

public sealed class UpdateBranchRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string CurrencyCode { get; init; } = "AED";
    public bool IsActive { get; init; } = true;
}
