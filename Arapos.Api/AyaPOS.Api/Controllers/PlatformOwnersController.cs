using Ayapos.Api.Contracts.Platform;
using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Ayapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("platform/owners")]
[Authorize(Policy = AuthPolicies.PlatformOnly)]
public sealed class PlatformOwnersController : ControllerBase
{
    private static readonly DateTime OwnerSentinelExpiryUtc = new(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    private readonly AyaposDbContext _db;
    private readonly PasswordHasherService _passwordHasher;

    public PlatformOwnersController(AyaposDbContext db, PasswordHasherService passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlatformOwnerDto>>> ListOwners(CancellationToken ct)
    {
        var items = await _db.Users
            .AsNoTracking()
            .Where(user => user.Role == "OWNER")
            .OrderBy(user => user.Username)
            .Select(user => new PlatformOwnerDto
            {
                Id = user.Id,
                Username = user.Username,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<PlatformOwnerDto>> CreateOwner([FromBody] CreatePlatformOwnerRequest request, CancellationToken ct)
    {
        var username = (request.Username ?? "").Trim();
        var password = (request.Password ?? "").Trim();

        if (username.Length < 3)
            return BadRequest("Username must be at least 3 characters.");

        if (password.Length < 6)
            return BadRequest("Password must be at least 6 characters.");

        var userExists = await _db.Users.AsNoTracking().AnyAsync(user => user.Username == username, ct);
        if (userExists)
            return Conflict("Username already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = _passwordHasher.Hash(password),
            Role = "OWNER",
            IsActive = request.IsActive,
            LicensePlan = "OWNER",
            LicenseStatus = "SYSTEM",
            LicenseStartedAt = DateTime.UtcNow,
            LicenseExpiresAt = OwnerSentinelExpiryUtc,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new PlatformOwnerDto
        {
            Id = user.Id,
            Username = user.Username,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("{ownerId:guid}/status")]
    public async Task<ActionResult<PlatformOwnerDto>> UpdateOwnerStatus(
        [FromRoute] Guid ownerId,
        [FromBody] UpdatePlatformOwnerStatusRequest request,
        CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == ownerId && item.Role == "OWNER", ct);
        if (user is null)
            return NotFound("Owner not found.");

        user.IsActive = request.IsActive;
        ApplyOwnerDefaults(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new PlatformOwnerDto
        {
            Id = user.Id,
            Username = user.Username,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("{ownerId:guid}/password")]
    public async Task<IActionResult> SetOwnerPassword(
        [FromRoute] Guid ownerId,
        [FromBody] SetPlatformOwnerPasswordRequest request,
        CancellationToken ct)
    {
        var newPassword = (request.NewPassword ?? "").Trim();
        if (newPassword.Length < 6)
            return BadRequest("New password must be at least 6 characters.");

        var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == ownerId && item.Role == "OWNER", ct);
        if (user is null)
            return NotFound("Owner not found.");

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        ApplyOwnerDefaults(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = "Owner password updated successfully.", user.Id });
    }

    private static void ApplyOwnerDefaults(User user)
    {
        user.Role = "OWNER";
        user.LicensePlan = "OWNER";
        user.LicenseStatus = "SYSTEM";
        user.LicenseExpiresAt = OwnerSentinelExpiryUtc;
    }
}
