using Arapos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Tenancy;

public sealed class TenantRouteMiddleware
{
    private readonly RequestDelegate _next;

    public TenantRouteMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AraposDbContext db)
    {
        // route: /t/{tenantSlug}/...
        if (context.Request.RouteValues.TryGetValue("tenantSlug", out var slugObj) &&
            slugObj is string slug &&
            !string.IsNullOrWhiteSpace(slug))
        {
            // if not authenticated, let auth handle it (401) on endpoints
            var user = context.User;
            var scope = user.FindFirst("scope")?.Value;

            // only enforce for tenant scope
            if (string.Equals(scope, "tenant", StringComparison.OrdinalIgnoreCase))
            {
                var tenantIdClaim = user.FindFirst("tenantId")?.Value;
                if (!Guid.TryParse(tenantIdClaim, out var tokenTenantId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid tenant token.");
                    return;
                }

                var tenant = await db.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Slug == slug && t.Status == "ACTIVE");

                if (tenant is null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync("Tenant not found.");
                    return;
                }

                if (tenant.Id != tokenTenantId)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Tenant mismatch.");
                    return;
                }
            }
        }

        await _next(context);
    }
}
