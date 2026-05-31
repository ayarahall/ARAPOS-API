namespace Ayapos.EndUser.Models.Auth;

public sealed class LoginApiResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public List<string> Permissions { get; set; } = [];
    public bool PermissionsConfigured { get; set; }
}
