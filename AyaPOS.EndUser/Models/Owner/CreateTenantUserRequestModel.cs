namespace Ayapos.EndUser.Models.Owner;

public sealed class CreateTenantUserRequestModel
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "TENANT";
    public string Password { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public string LicensePlan { get; set; } = "MONTHLY";
}
