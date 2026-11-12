using CafeManager.Core;
using CafeManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Web;

// Giao diện khách (MVC). Vùng làm việc của B. Dữ liệu hiện là dữ liệu giả (SampleMenu) theo hợp đồng API.
public sealed class HomeController : Controller
{
    // "/" và "/danh-muc" cùng là trang thực đơn (mục "Danh mục" trên thanh điều hướng).
    [HttpGet("/")]
    [HttpGet("/danh-muc")]
    public IActionResult Index(string? q, long? categoryId)
    {
        IEnumerable<MenuProductDto> products = MenuSource.Current;
        if (categoryId is long c) products = products.Where(p => p.CategoryId == c);
        if (!string.IsNullOrWhiteSpace(q))
            products = products.Where(p => p.ProductName.Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase));
        ViewData["Nav"] = Request.Path.StartsWithSegments("/danh-muc") ? "catalog" : "home";
        return View(new MenuPageModel(products.ToList(), MenuSource.Categories, q?.Trim(), categoryId));
    }

    [HttpGet("/don-cua-toi")]
    public IActionResult Orders()
    {
        ViewData["Nav"] = "orders";
        if (CustomerSession.Current(HttpContext) is null) return Redirect(CustomerSession.LoginUrl("/don-cua-toi"));
        return View();
    }
}

public sealed record MenuPageModel(IReadOnlyList<MenuProductDto> Products, IReadOnlyList<(long Id, string Name)> Categories,
    string? Query, long? CategoryId);
