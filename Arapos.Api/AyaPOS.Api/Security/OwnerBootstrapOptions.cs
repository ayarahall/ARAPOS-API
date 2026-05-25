namespace Ayapos.Api.Security;

public sealed class OwnerBootstrapOptions
{
    public bool Enabled { get; init; }
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
}
