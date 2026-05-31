namespace Ayapos.EndUser.Models.Customers;

public sealed class UpdateCustomerRequestModel
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
