namespace Ayapos.Api.Contracts.Platform;

public sealed class PlatformTenantUserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public string Role { get; init; } = "";
    public bool IsActive { get; init; }
    public string LicensePlan { get; init; } = "";
    public string LicenseStatus { get; init; } = "";
    public DateTime LicenseStartedAt { get; init; }
    public DateTime LicenseExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
