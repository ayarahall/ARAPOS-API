using Ayapos.Api.Data;
using Ayapos.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Controllers;

[ApiController]
[Route("t/{tenantSlug}/products-debug")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ProductsDebugController : ControllerBase
{
    private readonly AyaposDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ProductsDebugController(AyaposDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.TenantOnly)]
    public async Task<IActionResult> List()
    {
        if (!_environment.IsDevelopment())
            return NotFound();

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
