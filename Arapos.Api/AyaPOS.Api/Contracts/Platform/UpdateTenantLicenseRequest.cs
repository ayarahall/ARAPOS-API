namespace Ayapos.Api.Contracts.Platform;

public sealed class UpdateTenantLicenseRequest
{
    public string LicensePlan { get; init; } = "MONTHLY";
    public int MaxUsers { get; init; } = 1;
    public DateTime? LicenseStartedAt { get; init; }
    public DateTime? LicenseExpiresAt { get; init; }
}
