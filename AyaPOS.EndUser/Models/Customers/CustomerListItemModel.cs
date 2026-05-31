namespace Ayapos.EndUser.Models.Customers;

public sealed class CustomerListItemModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int InvoiceCount { get; set; }
    public DateTime? LastInvoiceAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
