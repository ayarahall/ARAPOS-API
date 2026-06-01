namespace Ayapos.Api.Contracts.Platform;

public sealed class BranchFeatureSettingsDto
{
    public bool AppointmentsRequireCustomer { get; set; }
    public bool AppointmentsPreventOverlap { get; set; }
    public bool AppointmentsAutoNoShow { get; set; }
    public bool AppointmentsCheckInCreatesInvoice { get; set; }
    public bool AppointmentsAllowNoShow { get; set; }
    public bool AppointmentsAllowCancel { get; set; }
    public bool ExpensesRequireApproval { get; set; }
    public bool ExpensesDeductCash { get; set; }
    public bool ExpensesNotifyApprovers { get; set; }
    public bool ExpensesAllowAiAssist { get; set; }
    public bool PosRequirePaymentReference { get; set; }
    public bool PosRequireAppointment { get; set; }
    public bool PosAutoPrintReceipt { get; set; }
    public bool PosAllowMultipleInvoiceTabs { get; set; }
}
