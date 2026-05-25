namespace Ayapos.Api.Contracts.Customers;

public sealed class UpdateCustomerRequest
{
    public string FullName { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}
