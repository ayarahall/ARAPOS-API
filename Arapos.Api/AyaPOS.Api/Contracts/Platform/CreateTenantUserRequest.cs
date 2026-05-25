namespace Ayapos.Api.Contracts.Platform;

public sealed class CreateTenantUserRequest
{
    public string Username { get; init; } = "";
    public string Role { get; init; } = "TENANT"; // TENANT/CASHIER
    public string? Password { get; init; }
    public string Pin { get; init; } = "";       // e.g. 1234
    public string LicensePlan { get; init; } = "MONTHLY";
}
