namespace Ayapos.EndUser.Models.Owner;

public sealed class PlatformTenantModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LicensePlan { get; set; } = string.Empty;
    public string LicenseStatus { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int AssignedUsers { get; set; }
    public DateTime LicenseStartedAt { get; set; }
    public DateTime LicenseExpiresAt { get; set; }
}
