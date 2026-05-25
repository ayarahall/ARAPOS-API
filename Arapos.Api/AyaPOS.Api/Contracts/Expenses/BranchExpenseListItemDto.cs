namespace Ayapos.Api.Contracts.Expenses;

public sealed class BranchExpenseListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "";
    public DateTime ExpenseDate { get; init; }
    public string Status { get; init; } = "";
    public string Notes { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}
