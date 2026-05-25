namespace Ayapos.Api.Contracts.Platform;

public sealed class PlatformTenantSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string Status { get; init; } = "";
    public string LicensePlan { get; init; } = "";
    public string LicenseStatus { get; init; } = "";
    public int MaxUsers { get; init; }
    public int AssignedUsers { get; set; }
    public DateTime LicenseStartedAt { get; init; }
    public DateTime LicenseExpiresAt { get; init; }
}
