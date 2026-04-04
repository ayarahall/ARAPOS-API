using Arapos.Api.Data;
using Arapos.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("auth-debug")]
public sealed class AuthDebugController : ControllerBase
{
    private readonly JwtTokenService _tokens;
    private readonly AraposDbContext _db;

    public AuthDebugController(
        JwtTokenService tokens,
        AraposDbContext db)
    {
        _tokens = tokens;
        _db = db;
    }

    // =========================
    // Platform Token
    // =========================
    [HttpPost("platform-token")]
    public IActionResult PlatformToken()
    {
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