namespace Ayapos.Api.Contracts.Cashier;

public sealed class CashierSessionDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public DateTime OpenedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public int OpeningCashCents { get; init; }
    public int TotalCashCents { get; init; }
    public int TotalCardCents { get; init; }
    public int TotalTransferCents { get; init; }
    public int TotalRefundCents { get; init; }
    public int CashExpenseCents { get; init; }
    public int ExpectedCashCents { get; init; }
    public int ActualCashCents { get; init; }
    public int DifferenceCents { get; init; }
    public string? DiscrepancyReason { get; init; }
    public int SalesInvoiceCount { get; init; }
    public int GrossSalesCents { get; init; }
    public int CollectedCents { get; init; }
    public bool IsClosed { get; init; }
    public IReadOnlyList<CashierSessionInvoiceDto> Invoices { get; init; } = [];
}
