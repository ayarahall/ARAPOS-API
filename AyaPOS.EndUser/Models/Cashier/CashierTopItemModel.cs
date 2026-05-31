namespace Ayapos.EndUser.Models.Cashier;

public sealed class CashierTopItemModel
{
    public string ItemType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int TotalCents { get; set; }
    public string CurrencyCode { get; set; } = "AED";
}
