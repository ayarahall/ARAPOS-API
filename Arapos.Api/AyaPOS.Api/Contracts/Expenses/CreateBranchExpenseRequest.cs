namespace Ayapos.Api.Contracts.Expenses;

public sealed class CreateBranchExpenseRequest
{
    public string Title { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal Amount { get; init; }
    public string? CurrencyCode { get; init; }
    public string? PaymentMethod { get; init; }
    public DateTime ExpenseDate { get; init; }
    public string? Notes { get; init; }
}
