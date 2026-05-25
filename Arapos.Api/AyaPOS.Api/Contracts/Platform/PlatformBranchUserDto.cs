namespace Ayapos.Api.Contracts.Platform;

public sealed class PlatformBranchUserDto
{
    public Guid Id { get; init; }
    public Guid BranchId { get; init; }
    public string Username { get; init; } = "";
    public string Role { get; init; } = "";
    public bool IsActive { get; init; }
    public string LicensePlan { get; init; } = "";
    public string LicenseStatus { get; init; } = "";
    public DateTime LicenseStartedAt { get; init; }
    public DateTime LicenseExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public bool PermissionsConfigured { get; init; }
}
