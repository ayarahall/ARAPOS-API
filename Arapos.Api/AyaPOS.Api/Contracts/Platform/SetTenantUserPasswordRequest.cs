namespace Ayapos.Api.Contracts.Platform;

public sealed class SetTenantUserPasswordRequest
{
    public string NewPassword { get; init; } = "";
}
