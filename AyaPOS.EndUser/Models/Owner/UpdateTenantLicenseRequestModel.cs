namespace Ayapos.EndUser.Models.Owner;

public sealed class UpdateTenantLicenseRequestModel
{
    public string LicensePlan { get; set; } = "MONTHLY";
    public int MaxUsers { get; set; } = 1;
}
