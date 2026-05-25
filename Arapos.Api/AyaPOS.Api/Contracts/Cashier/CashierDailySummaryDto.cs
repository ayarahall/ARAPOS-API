namespace Ayapos.Api.Contracts.Cashier;

public sealed class CashierDailySummaryDto
{
    public DateTime BusinessDateUtc { get; init; }
    public int InvoiceCount { get; init; }
    public int PostedInvoiceCount { get; init; }
    public int PaidInvoiceCount { get; init; }
    public int ActiveCustomerCount { get; init; }
    public int GrossSalesCents { get; init; }
    public int CollectedCents { get; init; }
    public int RemainingCents { get; init; }
    public IReadOnlyList<CashierTopItemDto> TopProducts { get; init; } = [];
    public IReadOnlyList<CashierTopItemDto> TopServices { get; init; } = [];
    public IReadOnlyList<CashierRecentPaymentDto> RecentPayments { get; init; } = [];
}
