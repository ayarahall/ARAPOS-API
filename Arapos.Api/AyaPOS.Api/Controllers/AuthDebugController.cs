using Ayapos.Api.Data;
using Ayapos.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("auth-debug")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class AuthDebugController : ControllerBase
{
    private readonly JwtTokenService _tokens;
    private readonly AyaposDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public AuthDebugController(
        JwtTokenService tokens,
        AyaposDbContext db,
        IWebHostEnvironment environment)
    {
        _tokens = tokens;
        _db = db;
        _environment = environment;
    }

    // =========================
    // Platform Token
    // =========================
    [HttpPost("platform-token")]
    public IActionResult PlatformToken()
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var token = _tokens.CreatePlatformToken(
            Guid.NewGuid(),
            "OWNER");

        return Ok(new { token });
    }

    // =========================
    // Tenant Token by SLUG
    // =========================
    [HttpPost("tenant-token/{slug}")]
    public async Task<IActionResult> TenantToken(string slug)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (tenant == null)
            return NotFound("Tenant not found");

        var token = _tokens.CreateTenantToken(
            Guid.NewGuid(),      // user id
            tenant.Id,           // tenant id الحقيقي
            "ADMIN");

        return Ok(new { token });
    }
}
