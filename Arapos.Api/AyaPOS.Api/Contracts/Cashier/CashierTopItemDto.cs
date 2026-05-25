namespace Ayapos.Api.Contracts.Cashier;

public sealed class CashierTopItemDto
{
    public string ItemType { get; init; } = "";
    public string Name { get; init; } = "";
    public int Quantity { get; init; }
    public int TotalCents { get; init; }
    public string CurrencyCode { get; init; } = "AED";
}
