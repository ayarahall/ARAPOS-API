using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public string LicensePlan { get; set; } = null!;

    public string LicenseStatus { get; set; } = null!;

    public DateTime LicenseStartedAt { get; set; }

    public DateTime LicenseExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? PinHash { get; set; }

    public string? PermissionsJson { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<BranchUserAssignment> BranchUserAssignments { get; set; } = new List<BranchUserAssignment>();

    public virtual ICollection<InventoryMove> InventoryMoves { get; set; } = new List<InventoryMove>();

    public virtual ICollection<Staff> LinkedStaffMembers { get; set; } = new List<Staff>();

    public virtual ICollection<UserPin> UserPins { get; set; } = new List<UserPin>();
}
