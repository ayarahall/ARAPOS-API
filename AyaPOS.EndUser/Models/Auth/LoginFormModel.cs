using System.ComponentModel.DataAnnotations;

namespace Ayapos.EndUser.Models.Auth;

public sealed class LoginFormModel
{
    [Required(ErrorMessage = "Tenant slug is required.")]
    public string TenantSlug { get; set; } = string.Empty;

    public string BranchId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;
}
