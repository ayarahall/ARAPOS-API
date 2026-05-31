namespace Ayapos.EndUser.Models.Owner;

public sealed class PlatformTenantUserModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string LicensePlan { get; set; } = string.Empty;
    public string LicenseStatus { get; set; } = string.Empty;
    public DateTime LicenseStartedAt { get; set; }
    public DateTime LicenseExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
