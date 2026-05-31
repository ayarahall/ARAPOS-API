namespace Ayapos.EndUser.Models.Expenses;

public sealed class AnalyzeExpenseReceiptResultModel
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string RawSummary { get; set; } = string.Empty;
}
