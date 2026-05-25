using System.ComponentModel.DataAnnotations;

namespace Ayapos.Api.Data.Entities;

public class CashierSession
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }

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

    public string? DiscrepancyReason { get; set; }

    public bool IsClosed { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}