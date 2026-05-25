namespace Ayapos.Api.Contracts.Platform;

public sealed class CreatePlatformOwnerRequest
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public bool IsActive { get; init; } = true;
}
