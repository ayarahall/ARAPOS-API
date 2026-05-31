namespace Ayapos.EndUser.Models.Cashier;

public sealed class CashierSessionModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int OpeningCashCents { get; set; }
    public int TotalCashCents { get; set; }
    public int TotalCardCents { get; set; }
    public int TotalTransferCents { get; set; }
    public int TotalRefundCents { get; set; }
    public int ExpectedCashCents { get; set; }
    public int ActualCashCents { get; set; }
    public int DifferenceCents { get; set; }
    public int SalesInvoiceCount { get; set; }
    public int GrossSalesCents { get; set; }
    public int CollectedCents { get; set; }
    public bool IsClosed { get; set; }
    public List<CashierSessionInvoiceModel> Invoices { get; set; } = [];
}
