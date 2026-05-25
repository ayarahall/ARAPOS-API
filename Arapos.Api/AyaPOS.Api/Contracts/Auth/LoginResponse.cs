namespace Ayapos.Api.Contracts.Auth;

public sealed class LoginResponse
{
    public string Token { get; init; } = "";
    public string Role { get; init; } = "";
    public Guid TenantId { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public bool PermissionsConfigured { get; init; }
}
    
