using Ayapos.Api.Data;
using Ayapos.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ayapos.Api.Security;

public sealed class OwnerBootstrapService
{
    private static readonly DateTime OwnerSentinelExpiryUtc = new(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    private readonly AyaposDbContext _db;
    private readonly PasswordHasherService _passwordHasher;
    private readonly OwnerBootstrapOptions _options;
    private readonly ILogger<OwnerBootstrapService> _logger;

    public OwnerBootstrapService(
        AyaposDbContext db,
        PasswordHasherService passwordHasher,
        IOptions<OwnerBootstrapOptions> options,
        ILogger<OwnerBootstrapService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureOwnerAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        var username = (_options.Username ?? "").Trim();
        var password = (_options.Password ?? "").Trim();

        if (username.Length < 3 || password.Length < 6)
        {
            _logger.LogWarning("Owner bootstrap skipped because username or password is not configured correctly.");
            return;
        }

        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        var now = DateTime.UtcNow;

        if (owner is null)
        {
            owner = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = _passwordHasher.Hash(password),
                Role = "OWNER",
                IsActive = true,
                LicensePlan = "OWNER",
                LicenseStatus = "SYSTEM",
                LicenseStartedAt = now,
                LicenseExpiresAt = OwnerSentinelExpiryUtc,
                CreatedAt = now
            };

            _db.Users.Add(owner);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Development owner account created: {Username}", username);
            return;
        }

        owner.Role = "OWNER";
        owner.IsActive = true;
        owner.PasswordHash = _passwordHasher.Hash(password);
        owner.LicensePlan = "OWNER";
        owner.LicenseStatus = "SYSTEM";
        owner.LicenseStartedAt = owner.LicenseStartedAt == default ? now : owner.LicenseStartedAt;
        owner.LicenseExpiresAt = OwnerSentinelExpiryUtc;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Development owner account refreshed: {Username}", username);
    }
}
