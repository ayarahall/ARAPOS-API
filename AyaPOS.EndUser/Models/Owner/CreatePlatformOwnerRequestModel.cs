namespace Ayapos.EndUser.Models.Owner;

public sealed class CreatePlatformOwnerRequestModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
