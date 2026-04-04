using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Arapos.Api.Tenancy;

public sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _http;

    public HttpTenantContext(IHttpContextAccessor http) => _http = http;

    public bool IsPlatform
    {
        get
        {
            var user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return false;

            return string.Equals(user.FindFirstValue("scope"), "platform", StringComparison.OrdinalIgnoreCase);
        }
    }

    public Guid? TenantId
    {
        get
        {
            var user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return null;

            if (!string.Equals(user.FindFirstValue("scope"), "tenant", StringComparison.OrdinalIgnoreCase))
                return null;

            var raw = user.FindFirstValue("tenantId");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public Guid? BranchId { get; set; }
}
