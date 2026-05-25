using System;

namespace Ayapos.Api.Data.Entities;

public partial class StaffDocument
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BranchId { get; set; }

    public Guid StaffId { get; set; }

    public string Title { get; set; } = null!;

    public string DocumentType { get; set; } = null!;

    public string? FileName { get; set; }

    public string? FileUrl { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;
}
