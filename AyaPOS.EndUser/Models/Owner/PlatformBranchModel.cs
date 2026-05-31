namespace Ayapos.EndUser.Models.Owner;

public sealed class PlatformBranchModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int AssignedUsers { get; set; }
    public bool HasPosWorkspace { get; set; }
    public bool HasAppointmentsWorkspace { get; set; }
    public bool HasExpensesWorkspace { get; set; }
    public bool HasPrintSettings { get; set; }
    public int NextInvoiceNumber { get; set; }
}
