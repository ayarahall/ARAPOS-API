using Arapos.Api.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("debug/tenant")]
public sealed class DebugTenantController : ControllerBase
{
    private readonly ITenantContext _tenant;

    public DebugTenantController(ITenantContext tenant) => _tenant = tenant;

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            scope = User.FindFirst("scope")?.Value,
            tenantIdClaim = User.FindFirst("tenantId")?.Value,
            resolvedTenantId = _tenant.TenantId,
            isPlatform = _tenant.IsPlatform
        });
    }
}
