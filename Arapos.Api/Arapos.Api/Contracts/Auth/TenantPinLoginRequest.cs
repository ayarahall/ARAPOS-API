namespace Arapos.Api.Contracts.Auth;

public sealed class TenantPinLoginRequest
{
    public string TenantSlug { get; init; } = "";
    public string Username { get; init; } = "";
    public string Pin { get; init; } = "";
}
