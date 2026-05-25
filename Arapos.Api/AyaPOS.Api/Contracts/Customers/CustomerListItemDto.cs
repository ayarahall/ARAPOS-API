namespace Ayapos.Api.Contracts.Customers;

public sealed class CustomerListItemDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = default!;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public int InvoiceCount { get; init; }
    public DateTime? LastInvoiceAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
