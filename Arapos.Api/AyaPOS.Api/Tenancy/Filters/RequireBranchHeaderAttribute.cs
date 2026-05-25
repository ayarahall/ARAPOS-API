using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Ayapos.Api.Data;

namespace Ayapos.Api.Tenancy.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireBranchHeaderAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Only apply to authenticated tenant scope
        var user = context.HttpContext.User;
        var scope = user.FindFirst("scope")?.Value;
        if (scope != "tenant")
        {
            await next();
            return;
        }

        // Read tenantId from token
        var tenantIdStr = user.FindFirst("tenantId")?.Value;
        if (!Guid.TryParse(tenantIdStr, out var tenantId))
        {
            context.Result = new UnauthorizedObjectResult("Missing/invalid tenantId claim.");
            return;
        }

        // Read branch header
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Branch-Id", out var branchHeader) ||
            !Guid.TryParse(branchHeader.ToString(), out var branchId))
        {
            context.Result = new BadRequestObjectResult("Missing/invalid X-Branch-Id header (must be GUID).");
            return;
        }

        // Validate branch belongs to tenant
        var db = context.HttpContext.RequestServices.GetRequiredService<AyaposDbContext>();
        var ok = await db.Branches.AsNoTracking()
            .AnyAsync(b => b.TenantId == tenantId && b.Id == branchId);

        if (!ok)
        {
            context.Result = new ForbidResult();
            return;
        }

        // store for later use (optional)
        context.HttpContext.Items["BranchId"] = branchId;

        await next();
    }
}
