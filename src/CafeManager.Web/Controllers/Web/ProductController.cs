using CafeManager.Core;
using CafeManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Web;

// B1: trang chi tiết món. Dữ liệu lấy từ MenuSource (cùng nguồn với trang thực đơn); thêm vào giỏ vẫn đi qua CartController
// nên mọi kiểm tra (số lượng, tùy chọn tối đa mỗi ly, món ngừng bán) dùng lại một chỗ, không tin dữ liệu từ trình duyệt.
public sealed class ProductController : Controller
{
    [HttpGet("/mon/{id:long}")]
    public IActionResult Detail(long id)
    {
        ViewData["Nav"] = "catalog";
        var product = MenuSource.Current.FirstOrDefault(p => p.ProductId == id);
        if (product is null)
        {
            // Món không còn bán hoặc không tồn tại: báo rõ và đưa khách về thực đơn.
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }
        return View(product);
    }
}

public sealed record VariantFormModel(MenuProductDto Product, MenuVariantDto Variant, string ReturnUrl);
