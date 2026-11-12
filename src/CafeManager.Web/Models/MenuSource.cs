using CafeManager.Core;

namespace CafeManager.Web.Models;

/// <summary>
/// Điểm DUY NHẤT lấy thực đơn cho giao diện khách. Hiện trả dữ liệu giả; cuối T4 đổi sang dữ liệu thật
/// (GET /api/menu của A) mà không phải sửa View hay giỏ hàng.
/// </summary>
public static class MenuSource
{
    // A gán nguồn thật (thực đơn từ CSDL) lúc khởi động Web; mặc định vẫn là dữ liệu giả để chạy giao diện không cần CSDL.
    public static Func<IReadOnlyList<MenuProductDto>> Provider { get; set; } = () => SampleMenu.Products;

    public static IReadOnlyList<MenuProductDto> Current => Provider();

    // Danh mục suy ra từ chính thực đơn đang hiển thị, nên mã danh mục luôn khớp dữ liệu thật.
    public static IReadOnlyList<(long Id, string Name)> Categories =>
        Current.Select(p => (Id: p.CategoryId, Name: p.CategoryName)).Distinct().OrderBy(c => c.Name).ToList();
}
