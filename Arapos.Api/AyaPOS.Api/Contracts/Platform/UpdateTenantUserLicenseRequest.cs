namespace Ayapos.Api.Contracts.Platform;

public sealed class UpdateTenantUserLicenseRequest
{
    public string LicensePlan { get; init; } = "MONTHLY";
    public DateTime? LicenseStartedAt { get; init; }
    public bool IsActive { get; init; } = true;
}
