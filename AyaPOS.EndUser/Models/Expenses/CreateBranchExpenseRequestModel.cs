namespace Ayapos.EndUser.Models.Expenses;

public sealed class CreateBranchExpenseRequestModel
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Operations";
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;
}
