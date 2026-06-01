using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class BranchSetting
{
    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public decimal MaxCashierDiscountPct { get; set; }

    public bool AllowLineDiscount { get; set; }

    public bool AllowInvoiceDiscount { get; set; }

    public bool RequireManagerForPriceOverride { get; set; }

    public string? CompanyName { get; set; }

    public string? CompanyLogoUrl { get; set; }

    public string? CompanyPhone { get; set; }

    public string? CompanyAddress { get; set; }

    public string? CompanyTaxNumber { get; set; }

    public string ReceiptTitle { get; set; } = null!;

    public string? ReceiptHeaderLine1 { get; set; }

    public string? ReceiptHeaderLine2 { get; set; }

    public string? ReceiptFooterNote { get; set; }

    public bool ShowBranchNameOnReceipt { get; set; }

    public bool ShowCustomerNameOnReceipt { get; set; }

    public bool ShowPaymentHistoryOnReceipt { get; set; }

    public bool AutoPrintReceiptAfterPayment { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Appointments feature flags
    public bool AppointmentsRequireCustomer { get; set; }
    public bool AppointmentsPreventOverlap { get; set; }
    public bool AppointmentsAutoNoShow { get; set; }
    public bool AppointmentsCheckInCreatesInvoice { get; set; }
    public bool AppointmentsAllowNoShow { get; set; }
    public bool AppointmentsAllowCancel { get; set; }

    // Expenses feature flags
    public bool ExpensesRequireApproval { get; set; }
    public bool ExpensesDeductCash { get; set; }
    public bool ExpensesNotifyApprovers { get; set; }
    public bool ExpensesAllowAiAssist { get; set; }

    // POS feature flags
    public bool PosRequirePaymentReference { get; set; }
    public bool PosRequireAppointment { get; set; }
    public bool PosAutoPrintReceipt { get; set; }
    public bool PosAllowMultipleInvoiceTabs { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
