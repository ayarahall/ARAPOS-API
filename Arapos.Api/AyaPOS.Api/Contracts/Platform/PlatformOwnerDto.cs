namespace Ayapos.Api.Contracts.Platform;

public sealed class PlatformOwnerDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
