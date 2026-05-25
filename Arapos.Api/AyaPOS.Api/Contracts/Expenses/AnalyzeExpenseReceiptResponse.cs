namespace Ayapos.Api.Contracts.Expenses;

public sealed class AnalyzeExpenseReceiptResponse
{
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "AED";
    public DateTime ExpenseDate { get; init; } = DateTime.UtcNow;
    public string Notes { get; init; } = string.Empty;
    public string VendorName { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public string RawSummary { get; init; } = string.Empty;
}
