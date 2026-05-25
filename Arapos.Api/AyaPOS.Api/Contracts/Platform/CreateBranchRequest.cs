namespace Ayapos.Api.Contracts.Platform;

public sealed class CreateBranchRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string CurrencyCode { get; init; } = "AED";
}
