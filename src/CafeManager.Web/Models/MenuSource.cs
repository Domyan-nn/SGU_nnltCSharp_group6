using CafeManager.Core;

namespace CafeManager.Web.Models;

/// <summary>
/// Điểm DUY NHẤT lấy thực đơn cho giao diện khách. Hiện trả dữ liệu giả; cuối T4 đổi sang dữ liệu thật
/// (GET /api/menu của A) mà không phải sửa View hay giỏ hàng.
/// </summary>
public static class MenuSource
{
    public static IReadOnlyList<MenuProductDto> Current => SampleMenu.Products;
}
