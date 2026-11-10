using CafeManager.Core;

namespace CafeManager.Web.Models;

// DỮ LIỆU GIẢ theo hợp đồng GET /api/menu (MenuProductDto). Chỉ dùng cho giao diện khi API của A chưa giao.
// Thay bằng dữ liệu thật: gọi dịch vụ thực đơn của máy chủ và bỏ file này.
public static class SampleMenu
{
    public static readonly IReadOnlyList<MenuProductDto> Products =
    [
        new(1, "Cà phê đen", 1, "Cà phê",
        [
            new(11, "Nhỏ", 25000, []),
            new(12, "Lớn", 32000, []),
        ]),
        new(2, "Cà phê sữa", 1, "Cà phê",
        [
            new(21, "Vừa", 30000, [new(91, "Thêm sữa", 5000, 2)]),
            new(22, "Lớn", 37000, [new(91, "Thêm sữa", 5000, 2), new(92, "Thêm shot cà phê", 8000, 2)]),
        ]),
        new(3, "Bạc xỉu", 1, "Cà phê",
        [
            new(31, "Vừa", 32000, [new(92, "Thêm shot cà phê", 8000, 1)]),
        ]),
        new(4, "Trà đào cam sả", 2, "Trà",
        [
            new(41, "Vừa", 38000, []),
            new(42, "Lớn", 45000, []),
        ]),
        new(5, "Trà sen vàng", 2, "Trà",
        [
            new(51, "Vừa", 35000, []),
        ]),
        new(6, "Sinh tố bơ", 3, "Sinh tố",
        [
            new(61, "Vừa", 42000, []),
        ]),
    ];

    public static readonly IReadOnlyList<(long Id, string Name)> Categories =
    [
        (1, "Cà phê"), (2, "Trà"), (3, "Sinh tố"),
    ];
}
