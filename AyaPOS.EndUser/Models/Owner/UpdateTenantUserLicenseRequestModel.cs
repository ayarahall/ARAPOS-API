namespace Ayapos.EndUser.Models.Owner;

public sealed class UpdateTenantUserLicenseRequestModel
{
    public string LicensePlan { get; set; } = "MONTHLY";
    public bool IsActive { get; set; } = true;
}
