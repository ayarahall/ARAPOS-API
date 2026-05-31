namespace Ayapos.EndUser.Models.Customers;

public sealed class CreateCustomerRequestModel
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
}
