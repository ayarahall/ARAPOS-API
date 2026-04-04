using Arapos.Api.Data;
using Arapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Controllers;

[ApiController]
[Route("t/{tenantSlug}/products-debug")]
public sealed class ProductsDebugController : ControllerBase
{
    private readonly AraposDbContext _db;

    public ProductsDebugController(AraposDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = AuthPolicies.TenantOnly)]
    public async Task<IActionResult> List()
    {
        var items = await _db.Products
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.TenantId,
                p.BranchId,
                p.Sku,
                p.Barcode,
                p.NameAr,
                p.NameEn,
                p.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }
}
