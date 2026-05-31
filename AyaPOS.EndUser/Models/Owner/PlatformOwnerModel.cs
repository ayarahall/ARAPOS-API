namespace Ayapos.EndUser.Models.Owner;

public sealed class PlatformOwnerModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
