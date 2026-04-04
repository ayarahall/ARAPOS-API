using Arapos.Api.Data;
using Arapos.Api.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireBranchAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        // branchId from middleware
        if (!http.Items.TryGetValue(BranchMiddleware.ItemKey, out var obj) || obj is not Guid branchId)
        {
            context.Result = new BadRequestObjectResult($"Missing {BranchMiddleware.HeaderName} header.");
            return;
        }

        // tenantId from JWT claim
        var tenantIdClaim = http.User.FindFirst("tenantId")?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Result = new ForbidResult();
            return;
        }

        // validate branch belongs to tenant
        var db = http.RequestServices.GetRequiredService<AraposDbContext>();

        var ok = await db.Branches.AsNoTracking()
            .AnyAsync(b => b.TenantId == tenantId && b.Id == branchId);

        if (!ok)
        {
            context.Result = new ForbidResult(); // 403
            return;
        }

        await next();
    }
}
