namespace Ayapos.Api.Contracts.Customers;

public sealed class CreateCustomerRequest
{
    public string FullName { get; init; } = default!;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
}
