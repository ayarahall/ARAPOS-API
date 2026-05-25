namespace Ayapos.Api.Contracts.Platform;

public sealed class CreateTenantRequest
{
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string LicensePlan { get; init; } = "MONTHLY";
    public int MaxUsers { get; init; } = 1;
}
