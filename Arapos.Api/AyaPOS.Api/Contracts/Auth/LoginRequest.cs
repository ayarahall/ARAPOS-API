namespace Ayapos.Api.Contracts.Auth;

public sealed class LoginRequest
{
    public string TenantSlug { get; set; } = default!;
    public Guid? BranchId { get; set; }
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}
