namespace Ayapos.Api.Data.Entities;

public partial class BranchExpense
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public string Title { get; set; } = null!;

    public string Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public DateTime ExpenseDate { get; set; }

    public string Status { get; set; } = null!;

    public string PaymentMethod { get; set; } = "cash";

    public DateTime? PaidAt { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
