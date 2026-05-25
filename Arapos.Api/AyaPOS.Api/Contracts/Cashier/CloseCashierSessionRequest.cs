namespace Ayapos.Api.Contracts.Cashier;

public sealed class CloseCashierSessionRequest
{
    public int ActualCashCents { get; init; }
    public string? DiscrepancyReason { get; init; }
}
