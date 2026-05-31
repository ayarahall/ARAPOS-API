namespace Ayapos.EndUser.Models.Cashier;

public sealed class CashierDailySummaryModel
{
    public DateTime BusinessDateUtc { get; set; }
    public int InvoiceCount { get; set; }
    public int PostedInvoiceCount { get; set; }
    public int PaidInvoiceCount { get; set; }
    public int ActiveCustomerCount { get; set; }
    public int GrossSalesCents { get; set; }
    public int CollectedCents { get; set; }
    public int RemainingCents { get; set; }
    public IReadOnlyList<CashierTopItemModel> TopProducts { get; set; } = [];
    public IReadOnlyList<CashierTopItemModel> TopServices { get; set; } = [];
    public IReadOnlyList<CashierRecentPaymentModel> RecentPayments { get; set; } = [];
}
