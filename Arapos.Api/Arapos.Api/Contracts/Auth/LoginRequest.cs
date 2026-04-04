namespace Arapos.Api.Contracts.Auth;

public sealed class LoginRequest
{
    public string TenantSlug { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}