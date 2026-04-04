namespace Arapos.Api.Contracts.Platform;

public sealed class CreateTenantRequest
{
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
}
