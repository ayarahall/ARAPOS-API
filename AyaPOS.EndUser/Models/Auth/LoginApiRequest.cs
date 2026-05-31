namespace Ayapos.EndUser.Models.Auth;

public sealed class LoginApiRequest
{
    public string TenantSlug { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
