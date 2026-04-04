namespace Arapos.Api.Contracts.Platform;

public sealed class CreateTenantUserRequest
{
    public string Username { get; init; } = "";
    public string Role { get; init; } = "ADMIN"; // ADMIN/CASHIER
    public string Pin { get; init; } = "";       // e.g. 1234
}
