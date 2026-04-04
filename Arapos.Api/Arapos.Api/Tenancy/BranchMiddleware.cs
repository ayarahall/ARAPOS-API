// FILE: Arapos.Api/Tenancy/BranchMiddleware.cs

using Arapos.Api.Data;
using Arapos.Api.Tenancy.Filters;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Tenancy;

public sealed class BranchMiddleware
{
    // ✅ Public so controllers/filters can reuse them
    public const string HeaderName = "X-Branch-Id";
    public const string ItemKey = "BranchId";

    private readonly RequestDelegate _next;

    public BranchMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantCtx, AraposDbContext db)
    {
        // Platform requests: no branch required
        if (tenantCtx.IsPlatform || tenantCtx.TenantId is null)
        {
            await _next(context);
            return;
        }

        // Enforce only for endpoints decorated with [RequireBranchHeader]
        var endpoint = context.GetEndpoint();
        var requiresBranch = endpoint?.Metadata?.GetMetadata<RequireBranchHeaderAttribute>() is not null;

        if (!requiresBranch)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest,
                "MissingBranchHeader",
                $"{HeaderName} header is required.");
            return;
        }

        if (!Guid.TryParse(raw.ToString(), out var branchId))
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest,
                "InvalidBranchHeader",
                $"{HeaderName} must be a valid GUID.");
            return;
        }

        // Validate branch belongs to tenant (safe vs query filters)
        var ok = await db.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(b => b.TenantId == tenantCtx.TenantId.Value && b.Id == branchId);

        if (!ok)
        {
            await WriteProblem(context, StatusCodes.Status403Forbidden,
                "BranchForbidden",
                "Branch does not belong to this tenant.");
            return;
        }

        // ✅ Put in tenant context + HttpContext.Items for any existing code
        tenantCtx.BranchId = branchId;
        context.Items[ItemKey] = branchId;

        await _next(context);
    }

    private static async Task WriteProblem(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new { error = code, message });
    }
}
