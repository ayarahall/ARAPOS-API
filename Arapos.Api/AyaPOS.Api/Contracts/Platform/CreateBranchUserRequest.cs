namespace Ayapos.Api.Contracts.Platform;

public sealed class CreateBranchUserRequest
{
    public string Username { get; init; } = "";
    public string Role { get; init; } = "CASHIER";
    public string? Password { get; init; }
    public string Pin { get; init; } = "";
    public string LicensePlan { get; init; } = "MONTHLY";
}
