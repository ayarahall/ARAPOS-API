namespace Ayapos.EndUser.Models.Owner;

public sealed class CreatePlatformTenantRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string LicensePlan { get; set; } = "MONTHLY";
    public int MaxUsers { get; set; } = 1;
}
