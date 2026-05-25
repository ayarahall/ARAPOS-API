using Ayapos.Api.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("debug/tenant")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class DebugTenantController : ControllerBase
{
    private readonly ITenantContext _tenant;
    private readonly IWebHostEnvironment _environment;

    public DebugTenantController(ITenantContext tenant, IWebHostEnvironment environment)
    {
        _tenant = tenant;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Get()
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        return Ok(new
        {
            scope = User.FindFirst("scope")?.Value,
            tenantIdClaim = User.FindFirst("tenantId")?.Value,
            resolvedTenantId = _tenant.TenantId,
            isPlatform = _tenant.IsPlatform
        });
    }
}
