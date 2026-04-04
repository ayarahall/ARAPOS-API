using System;
using System.Collections.Generic;

namespace Arapos.Api.Data.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? PinHash { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<InventoryMove> InventoryMoves { get; set; } = new List<InventoryMove>();

    public virtual ICollection<UserPin> UserPins { get; set; } = new List<UserPin>();
}
