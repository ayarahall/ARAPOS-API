using Arapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("platform/ping")]
public sealed class PlatformPingController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthPolicies.PlatformOnly)]
    public IActionResult Get()
        => Ok(new { ok = true, scope = "platform", nowUtc = DateTime.UtcNow });
}
