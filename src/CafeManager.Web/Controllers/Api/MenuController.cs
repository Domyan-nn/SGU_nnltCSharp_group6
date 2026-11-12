using CafeManager.Core;
using CafeManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Api;

// UC01: thực đơn công khai (món đang bán, giá hiện hành, tùy chọn). Giá luôn do máy chủ tính.
[ApiController]
[Route("api/menu")]
[AllowAnonymous]
public sealed class MenuController(MenuQueryService menu) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<MenuProductDto>> Get(long? categoryId, string? q, CancellationToken ct)
    {
        IEnumerable<MenuProductDto> products = await menu.GetAsync(DateTime.UtcNow, ct);
        if (categoryId is long c) products = products.Where(p => p.CategoryId == c);
        if (!string.IsNullOrWhiteSpace(q)) products = products.Where(p => p.ProductName.Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase));
        return products.ToList();
    }
}
