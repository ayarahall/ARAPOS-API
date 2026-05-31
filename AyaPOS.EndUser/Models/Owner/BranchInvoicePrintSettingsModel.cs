namespace Ayapos.EndUser.Models.Owner;

public sealed class BranchInvoicePrintSettingsModel
{
    public Guid BranchId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyTaxNumber { get; set; }
    public string ReceiptTitle { get; set; } = "Sales Receipt";
    public string? ReceiptHeaderLine1 { get; set; }
    public string? ReceiptHeaderLine2 { get; set; }
    public string? ReceiptFooterNote { get; set; }
    public bool ShowBranchNameOnReceipt { get; set; } = true;
    public bool ShowCustomerNameOnReceipt { get; set; } = true;
    public bool ShowPaymentHistoryOnReceipt { get; set; } = true;
    public bool AutoPrintReceiptAfterPayment { get; set; }
}
