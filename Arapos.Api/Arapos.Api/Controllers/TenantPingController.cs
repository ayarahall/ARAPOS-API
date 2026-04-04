using Arapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("t/{tenantSlug}/ping")]
public sealed class TenantPingController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthPolicies.TenantOnly)]
    public IActionResult Ping([FromRoute] string tenantSlug)
        => Ok(new { ok = true, tenantSlug });
}
