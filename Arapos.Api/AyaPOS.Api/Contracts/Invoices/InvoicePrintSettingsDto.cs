namespace Ayapos.Api.Contracts.Invoices;

public sealed class InvoicePrintSettingsDto
{
    public string? CompanyName { get; init; }
    public string? CompanyLogoUrl { get; init; }
    public string? CompanyPhone { get; init; }
    public string? CompanyAddress { get; init; }
    public string? CompanyTaxNumber { get; init; }
    public string ReceiptTitle { get; init; } = "Sales Receipt";
    public string? ReceiptHeaderLine1 { get; init; }
    public string? ReceiptHeaderLine2 { get; init; }
    public string? ReceiptFooterNote { get; init; }
    public bool ShowBranchNameOnReceipt { get; init; } = true;
    public bool ShowCustomerNameOnReceipt { get; init; } = true;
    public bool ShowPaymentHistoryOnReceipt { get; init; } = true;
    public bool AutoPrintReceiptAfterPayment { get; init; }
}
