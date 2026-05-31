namespace Ayapos.EndUser.Models.Owner;

public sealed class PlatformBranchUserModel
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string LicensePlan { get; set; } = string.Empty;
    public string LicenseStatus { get; set; } = string.Empty;
    public DateTime LicenseStartedAt { get; set; }
    public DateTime LicenseExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Permissions { get; set; } = [];
    public bool PermissionsConfigured { get; set; }
}
