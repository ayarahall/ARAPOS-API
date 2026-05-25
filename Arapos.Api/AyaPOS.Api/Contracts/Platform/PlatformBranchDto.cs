namespace Ayapos.Api.Contracts.Platform;

public sealed class PlatformBranchDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string CurrencyCode { get; init; } = "";
    public bool IsActive { get; init; }
    public int AssignedUsers { get; set; }
    public bool HasPosWorkspace { get; init; }
    public bool HasAppointmentsWorkspace { get; init; }
    public bool HasExpensesWorkspace { get; init; }
    public bool HasPrintSettings { get; init; }
    public int NextInvoiceNumber { get; init; }
}
