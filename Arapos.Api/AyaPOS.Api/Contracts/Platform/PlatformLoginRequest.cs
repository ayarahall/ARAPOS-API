namespace Ayapos.Api.Contracts.Platform;

public sealed class PlatformLoginRequest
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
}
